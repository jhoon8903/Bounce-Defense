using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Core.Random;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 lifecycle 오케스트레이터: 스폰/디스폰 명단 + 틱 구동 + 로스터(어떤 볼을 쏘나)만 담당.
    //  - '언제 쏘나'는 BallFiringScheduler, '어떻게 움직이나'는 IBallMotor, '데미지 규칙'은 DamageResolver 소유.
    //  - 로스터 = 기본 노멀 5 + 획득 액티브당 1(스킬 볼). SkillRuntime이 로드아웃 변경 시 SetRoster로 밀어넣는다.
    //    각 스폰은 결원(desired>current) 타입을 채워 동시 비행 구성이 로스터에 수렴한다("액티브가 볼을 추가").
    // 실시간 연속 모델: 좌·우·천장 벽은 정상 반사, 바닥에 닿으면 반사 대신 Char(고정 원점)로 수집.
    public sealed class BallController : BaseController, IBallEffectContext
    {
        private const int MaxBounces = 40;
        private const int DefaultBallCount = 5; // 기본 노멀 볼 수(스펙 정정: 기본 5 + 카드당 +1).
        // 수집 도착 판정 거리(이 안쪽이면 Char에 닿은 것으로 보고 소멸).
        private const float CollectArrivalDist = 0.15f;
        private static readonly Vector2 OutOfBoundsMin = new(-6f, -11f);
        private static readonly Vector2 OutOfBoundsMax = new(6f, 11f);
        // Char 본체 월드 좌표(위치 고정). LaunchController가 SetCollectTarget으로 주입, 미배선 시 폴백.
        private static readonly Vector2 DefaultCollectTarget = new(0f, -6.70f);

        private readonly IBallFactory _factory;
        private readonly IClock _clock;
        private readonly BallConfig _config;
        private readonly DamageResolver _resolver;
        private readonly CombatEventHub _hub;
        private readonly IRandom _random;
        private readonly GridController _grid; // Laser 행뎀 조회(배치 권한 소유자). 히트 시점에만 질의 — 초기화 순서 무관.
        private readonly BallFiringScheduler _scheduler = new();
        private readonly Dictionary<string, BallModel> _models = new();
        private readonly Dictionary<string, BallView> _views = new();
        private readonly Dictionary<string, IBallMotor> _motors = new();
        private readonly Dictionary<string, BallSpawnSpec> _specs = new(); // 볼별 타입/데미지/모듈
        // 바닥을 맞고 Char로 귀환 중인 볼(모터 스텝 대신 직선 홈잉). 도착 시 소멸.
        private readonly HashSet<string> _collecting = new();
        private readonly List<string> _idCache = new();
        private readonly HashSet<int> _rowHitHandles = new(); // Laser 행뎀 dedup(멀티셀 블록 1회) 재사용 버퍼
        // Cluster 분열 특수볼(로스터 미집계): 틱·데미지·바닥수집은 정상이나 desired/inFlight 회계에서 제외.
        private readonly HashSet<string> _unmanaged = new();
        // Magic Mirror(패시브): 벽튕김한 볼을 다음 직격용으로 무장, 첫 적히트에 소비형 가산% 부여(§209·§184).
        private readonly HashSet<string> _mirrorArmed = new();
        private float _mirrorPercent; // 0 = Magic Mirror 미보유(무장 안 함)

        // 로스터(쏠 볼 사양)와 타입별 desired/current 집계 — 결원 채우기로 구성 수렴.
        private readonly List<BallSpawnSpec> _roster = new();
        private readonly Dictionary<BallSourceType, int> _desiredByType = new();
        private readonly Dictionary<BallSourceType, int> _inFlightByType = new();

        private int _wallMask;
        private int _enemyMask;
        private int _blockMask;
        private Vector2 _collectTarget = DefaultCollectTarget;

        public BallController(IBallFactory factory, IClock clock, BallConfig config, DamageResolver resolver, CombatEventHub hub, IRandom random, GridController grid)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
            _resolver = resolver;
            _hub = hub;
            _random = random;
            _grid = grid;
        }

        // ---- IBallEffectContext (볼 모듈이 온-히트에 쓰는 코어 밖 서비스 파사드) ----
        public IRandom Random => _random;

        // Laser: 히트 적과 같은 그리드 행의 다른 적들에게 flat 2차뎀. 열 오름차순(결정론) + 블록 핸들 dedup.
        public void DamageEnemyRow(IDamageable originEnemy, float flatDamage, BallSourceType source)
        {
            if (_resolver == null || _grid == null || !_grid.IsReady || flatDamage <= 0f) return;
            if (!(originEnemy is EnemyView originView) || originView.Model == null) return;
            int row = _grid.Model.WorldToCell(originView.Model.Position).Row;
            int cols = _grid.Cols;
            _rowHitHandles.Clear();
            for (int col = 0; col < cols; col++)
            {
                int handle = _grid.OccupantHandleAt(col, row);
                if (handle == GridMap.Empty || !_rowHitHandles.Add(handle)) continue; // 빈칸/이미 맞은 블록 스킵
                if (!_grid.TryGetOccupant(handle, out IDamageable occ) || occ == originEnemy) continue; // 직격 대상 제외
                Vector2 pos = occ is EnemyView ev && ev.Model != null ? ev.Model.Position : Vector2.zero;
                HitContext ctx = HitContext.Secondary(occ, source, DamageKind.LaserRow, flatDamage);
                _resolver.Resolve(ctx); // 무크리·무버프 flat, 동일 리졸버 → 사망 시 RaiseKill(Last Match 등 이어짐, §255)
                if (ctx.FinalDamage > 0 && occ is EnemyView ev2) _hub?.RaiseHit(ev2, pos, ctx.FinalDamage, ctx.IsCrit);
            }
        }

        // Magic Mirror 보유% 주입(SkillRuntime, 로드아웃 변경 시). 0 = 미보유(무장 비활성). 레벨별 +20/40/60%.
        public void SetMirrorPercent(float percent) => _mirrorPercent = Mathf.Max(0f, percent);

        // Char 본체(고정 발사·수집 원점) 주입. 미호출 시 DefaultCollectTarget 사용.
        public void SetCollectTarget(Vector2 target) => _collectTarget = target;

        // ---- 연속 자동발사(스케줄러 위임). 시작 후 멈추지 않는다(실시간 연속발사 설계) ----
        public void StartFiring(Vector2 direction) => _scheduler.Start(direction);
        public void SetFireDirection(Vector2 direction) => _scheduler.SetDirection(direction);

        // 로드아웃 → 볼 로스터 갱신(SkillRuntime 호출). 비어 있으면 기본 노멀 5로 폴백(코어 루프 항상 동작).
        public void SetRoster(IReadOnlyList<BallSpawnSpec> specs)
        {
            _roster.Clear();
            if (specs != null && specs.Count > 0) _roster.AddRange(specs);
            else BuildDefaultRoster();
            RecomputeDesired();
        }

        protected override void OnInitialize()
        {
            _wallMask = LayerMask.GetMask("Wall");
            _enemyMask = LayerMask.GetMask("Enemy");
            _blockMask = LayerMask.GetMask("Block");
            if (_roster.Count == 0) { BuildDefaultRoster(); RecomputeDesired(); }
            _clock.OnFixedTick += OnClockFixedTick;
        }

        protected override void OnDispose()
        {
            _clock.OnFixedTick -= OnClockFixedTick;
            ReleaseAll();
        }

        protected override void OnReset() => ReleaseAll();

        protected override void OnTick(float deltaTime) { }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_scheduler.TryFire(fixedDeltaTime, _models.Count, _roster.Count) && TryPickSpec(out BallSpawnSpec spec))
                Spawn(_collectTarget, _scheduler.Direction, spec);
            if (_models.Count == 0) return;

            float collectSpeed = _config != null ? _config.Speed : 12f;
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--)
            {
                string id = _idCache[i];
                if (!_models.TryGetValue(id, out BallModel model)) continue;

                // 수집 중: 모터 물리 대신 Char로 직선 홈잉, 도착 시 소멸.
                if (_collecting.Contains(id))
                {
                    Vector2 toTarget = _collectTarget - model.Position;
                    float step = collectSpeed * fixedDeltaTime;
                    if (toTarget.magnitude <= Mathf.Max(step, CollectArrivalDist)) Release(id);
                    else model.SetPosition(model.Position + toTarget.normalized * step);
                    continue;
                }

                if (!_motors.TryGetValue(id, out IBallMotor motor)) continue;
                BallMotorStepResult result = motor.Step(fixedDeltaTime);
                model.SetPosition(motor.Position);
                if (result.BounceCountThisStep > 0) model.RegisterBounce(result.BounceCountThisStep);

                // 볼→데미지: 이번 스텝의 모든 적/블록 접촉에 데미지 적용(모터가 콜라이더 dedup).
                // 바닥 수집으로 continue하기 전에 처리 — 같은 스텝에 적+바닥을 맞아도 데미지는 살린다.
                ResolveDamageHits(id, motor);

                // Magic Mirror: 이번 스텝 벽튕김 시 '다음' 직격용으로 무장(현재 스텝 히트는 위에서 이미 소비 판정 끝).
                if (_mirrorPercent > 0f && result.WallBounceCountThisStep > 0) _mirrorArmed.Add(id);

                // 바닥면 반사면 반사 대신 수집(Char로 귀환) — 손실 없는 result.HitFloor 사용(코너 다중바운스에도 정확).
                if (result.HitFloor) { _collecting.Add(id); continue; }
                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public BallModel Spawn(Vector2 origin, Vector2 direction, BallSpawnSpec spec) =>
            SpawnInternal(origin, direction, spec, managed: true);

        // Cluster 분열(services.SpawnClusterBall): 히트 위치서 무작위 상향 특수볼 1개.
        //  - 2차뎀(무크리)·무모듈(무재귀 §183)·Cluster 타입(Warm Tin 대상 아님)·로스터 미집계(unmanaged).
        public void SpawnClusterBall(Vector2 origin, float damage)
        {
            if (damage <= 0f) return;
            // 무작위 상향 방향(결정론 RNG). 아래로 쏘면 즉시 바닥수집돼 낭비 → 20~160°.
            float t = _random != null ? _random.NextFloat() : 0.5f;
            float angle = Mathf.Deg2Rad * (20f + t * 140f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            BallSpawnSpec spec = new BallSpawnSpec(BallSourceType.Cluster, damage, null, false, DamageKind.ClusterSpawn);
            SpawnInternal(origin, dir, spec, managed: false);
        }

        private BallModel SpawnInternal(Vector2 origin, Vector2 direction, BallSpawnSpec spec, bool managed)
        {
            float speed = _config != null ? _config.Speed : 12f;
            float radius = _config != null ? _config.Radius : 0.15f;

            string id = _factory.GenerateId();
            (BallModel model, BallView view) = _factory.Create(origin, spec.SourceType);
            if (model == null || view == null) return null;

            // Ghost 관통 = passThroughMask에 Enemy 레이어(반사 없이 통과·데미지는 기록). 나머지는 0(정상 반사).
            int passThroughMask = spec.PenetratesEnemies ? _enemyMask : 0;
            IBallMotor motor = new KinematicRaycastMotor();
            motor.Init(origin, direction, speed, radius, _wallMask, _enemyMask, _blockMask, passThroughMask);

            _models[id] = model;
            _views[id] = view;
            _motors[id] = motor;
            _specs[id] = spec;
            if (managed) _inFlightByType[spec.SourceType] = InFlightOf(spec.SourceType) + 1;
            else _unmanaged.Add(id); // 분열 특수볼 — 로스터 desired/inFlight 미집계
            return model;
        }

        // 이번 스텝의 적/블록 접촉마다 데미지 적용. 벽/바닥은 대상 아님.
        // 볼별 사양(spec)의 baseDamage로 HitContext(직격)를 만들어 리졸버가 가산%·크리·단일반올림 처리,
        // 데미지 확정 후 볼 모듈(있으면)의 온-히트 훅 발화(예: Fire → 번 부여).
        private void ResolveDamageHits(string id, IBallMotor motor)
        {
            if (_resolver == null) return;
            IReadOnlyList<BallHit> hits = motor.LastStepHits;
            if (hits == null || hits.Count == 0) return;

            BallSpawnSpec spec = _specs.TryGetValue(id, out BallSpawnSpec s) ? s : DefaultSpec();
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D collider = hits[i].Collider;
                if (collider == null) continue;
                Vector2 pos = collider.transform.position; // Resolve 전 캡처(살상타 디스폰 대비, 숫자는 살린다).
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target == null) continue; // IDamageable 없는 대상(예: 브리지 미배선 블록) — 안전 무시.
                // 로스터 볼 = 직격(크리·모디파이어·hitNormal 전후면). Cluster 특수볼 등 2차볼 = 무크리·무버프 flat.
                HitContext ctx = spec.DamageKind == DamageKind.Direct
                    ? HitContext.Direct(target, spec.SourceType, spec.BaseDamage, hits[i].Normal)
                    : HitContext.Secondary(target, spec.SourceType, spec.DamageKind, spec.BaseDamage);
                // Magic Mirror 소비: 무장된 볼의 '첫' 직격에만 가산 부여 후 해제(Ghost 관통 다중히트도 첫 적만, §184).
                if (spec.DamageKind == DamageKind.Direct && _mirrorArmed.Remove(id))
                    ctx.BonusAdditivePercent = _mirrorPercent;
                _resolver.Resolve(ctx);
                if (target is EnemyView ev) _hub?.RaiseHit(ev, pos, ctx.FinalDamage, ctx.IsCrit);
                spec.Module?.OnEnemyHit(target, ctx, this); // services = 이 컨트롤러(IBallEffectContext 파사드)
            }
        }

        public void Release(string id)
        {
            if (!_models.TryGetValue(id, out BallModel model)) return;
            _views.TryGetValue(id, out BallView view);
            _factory.Release(model, view);
            // unmanaged(분열 특수볼)는 inFlight에 안 세었으므로 감산 제외. 로스터 볼만 감산.
            if (!_unmanaged.Remove(id) && _specs.TryGetValue(id, out BallSpawnSpec spec))
                _inFlightByType[spec.SourceType] = Mathf.Max(0, InFlightOf(spec.SourceType) - 1);
            _models.Remove(id);
            _views.Remove(id);
            _motors.Remove(id);
            _specs.Remove(id);
            _collecting.Remove(id);
            _mirrorArmed.Remove(id); // 무장 상태로 despawn되면 정리(풀 재사용 stale 방지)
        }

        public void ReleaseAll()
        {
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--) Release(_idCache[i]);
        }

        // ---- 로스터 헬퍼 ----
        private void BuildDefaultRoster()
        {
            _roster.Clear();
            BallSpawnSpec normal = DefaultSpec();
            for (int i = 0; i < DefaultBallCount; i++) _roster.Add(normal);
        }

        // 로스터 미설정(SkillRuntime 미배선) 시에만 쓰는 방어용 폴백. 실제 노멀 뎀은 SkillRuntime이 SetRoster로
        // 항상 주입(NormalBallDamage 상수)하므로 게임플레이에선 도달하지 않는다 — 여기 dmg=0은 "SkillRuntime 미배선" 신호.
        private BallSpawnSpec DefaultSpec() => new BallSpawnSpec(BallSourceType.Normal, 0f, null);

        private void RecomputeDesired()
        {
            _desiredByType.Clear();
            for (int i = 0; i < _roster.Count; i++)
            {
                BallSourceType t = _roster[i].SourceType;
                _desiredByType[t] = (_desiredByType.TryGetValue(t, out int c) ? c : 0) + 1;
            }
        }

        // 결원(현재 비행 수 < 로스터 목표 수)인 첫 타입의 사양을 낸다. 없으면 false(캡 도달).
        private bool TryPickSpec(out BallSpawnSpec spec)
        {
            for (int i = 0; i < _roster.Count; i++)
            {
                BallSourceType t = _roster[i].SourceType;
                int desired = _desiredByType.TryGetValue(t, out int d) ? d : 0;
                if (InFlightOf(t) < desired) { spec = _roster[i]; return true; }
            }
            spec = default;
            return false;
        }

        private int InFlightOf(BallSourceType type) => _inFlightByType.TryGetValue(type, out int c) ? c : 0;

        private static bool IsOutOfBounds(Vector2 pos)
        {
            return pos.x < OutOfBoundsMin.x || pos.x > OutOfBoundsMax.x ||
                   pos.y < OutOfBoundsMin.y || pos.y > OutOfBoundsMax.y;
        }
    }
}
