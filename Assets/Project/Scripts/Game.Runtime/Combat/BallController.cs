using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 lifecycle 오케스트레이터: 스폰/디스폰 명단 + 틱 구동 + 로스터(어떤 볼을 쏘나)만 담당.
    //  - '언제 쏘나'는 BallFiringScheduler, '어떻게 움직이나'는 IBallMotor, '데미지 규칙'은 DamageResolver 소유.
    //  - 로스터 = 기본 노멀 5 + 획득 액티브당 1(스킬 볼). SkillRuntime이 로드아웃 변경 시 SetRoster로 밀어넣는다.
    //    각 스폰은 결원(desired>current) 타입을 채워 동시 비행 구성이 로스터에 수렴한다("액티브가 볼을 추가").
    // 실시간 연속 모델: 좌·우·천장 벽은 정상 반사, 바닥에 닿으면 반사 대신 Char(고정 원점)로 수집.
    public sealed class BallController : BaseController
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
        private readonly BallFiringScheduler _scheduler = new();
        private readonly Dictionary<string, BallModel> _models = new();
        private readonly Dictionary<string, BallView> _views = new();
        private readonly Dictionary<string, IBallMotor> _motors = new();
        private readonly Dictionary<string, BallSpawnSpec> _specs = new(); // 볼별 타입/데미지/모듈
        // 바닥을 맞고 Char로 귀환 중인 볼(모터 스텝 대신 직선 홈잉). 도착 시 소멸.
        private readonly HashSet<string> _collecting = new();
        private readonly List<string> _idCache = new();

        // 로스터(쏠 볼 사양)와 타입별 desired/current 집계 — 결원 채우기로 구성 수렴.
        private readonly List<BallSpawnSpec> _roster = new();
        private readonly Dictionary<BallSourceType, int> _desiredByType = new();
        private readonly Dictionary<BallSourceType, int> _inFlightByType = new();

        private int _wallMask;
        private int _enemyMask;
        private int _blockMask;
        private Vector2 _collectTarget = DefaultCollectTarget;

        public BallController(IBallFactory factory, IClock clock, BallConfig config, DamageResolver resolver, CombatEventHub hub)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
            _resolver = resolver;
            _hub = hub;
        }

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

                // 바닥면 반사면 반사 대신 수집(Char로 귀환) — 손실 없는 result.HitFloor 사용(코너 다중바운스에도 정확).
                if (result.HitFloor) { _collecting.Add(id); continue; }
                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public BallModel Spawn(Vector2 origin, Vector2 direction, BallSpawnSpec spec)
        {
            float speed = _config != null ? _config.Speed : 12f;
            float radius = _config != null ? _config.Radius : 0.15f;

            string id = _factory.GenerateId();
            (BallModel model, BallView view) = _factory.Create(origin, spec.SourceType);
            if (model == null || view == null) return null;

            IBallMotor motor = new KinematicRaycastMotor();
            motor.Init(origin, direction, speed, radius, _wallMask, _enemyMask, _blockMask, passThroughMask: 0);

            _models[id] = model;
            _views[id] = view;
            _motors[id] = motor;
            _specs[id] = spec;
            _inFlightByType[spec.SourceType] = InFlightOf(spec.SourceType) + 1;
            return model;
        }

        // 이번 스텝의 적/블록 접촉마다 데미지 적용. 벽/바닥은 대상 아님.
        // 볼별 사양(spec)의 baseDamage로 HitContext(직격)를 만들어 리졸버가 가산%·크리·단일반올림 처리,
        // 데미지 확정 후 볼 모듈(있으면)의 온-히트 훅 발화(예: Fire → 번 부여).
        private void ResolveDamageHits(string id, IBallMotor motor)
        {
            if (_resolver == null) return;
            IReadOnlyList<Collider2D> hits = motor.LastStepHits;
            if (hits == null || hits.Count == 0) return;

            BallSpawnSpec spec = _specs.TryGetValue(id, out BallSpawnSpec s) ? s : DefaultSpec();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] == null) continue;
                Vector2 pos = hits[i].transform.position; // Resolve 전 캡처(살상타 디스폰 대비, 숫자는 살린다).
                IDamageable target = hits[i].GetComponentInParent<IDamageable>();
                if (target == null) continue; // IDamageable 없는 대상(예: 브리지 미배선 블록) — 안전 무시.
                HitContext ctx = HitContext.Direct(target, spec.SourceType, spec.BaseDamage);
                _resolver.Resolve(ctx);
                if (target is EnemyView ev) _hub?.RaiseHit(ev, pos, ctx.FinalDamage, ctx.IsCrit);
                spec.Module?.OnEnemyHit(target, ctx);
            }
        }

        public void Release(string id)
        {
            if (!_models.TryGetValue(id, out BallModel model)) return;
            _views.TryGetValue(id, out BallView view);
            _factory.Release(model, view);
            if (_specs.TryGetValue(id, out BallSpawnSpec spec))
                _inFlightByType[spec.SourceType] = Mathf.Max(0, InFlightOf(spec.SourceType) - 1);
            _models.Remove(id);
            _views.Remove(id);
            _motors.Remove(id);
            _specs.Remove(id);
            _collecting.Remove(id);
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

        private BallSpawnSpec DefaultSpec()
        {
            float dmg = _config != null ? _config.GetDamage(1) : 0f;
            return new BallSpawnSpec(BallSourceType.Normal, dmg, null);
        }

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
