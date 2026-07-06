using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 lifecycle 오케스트레이터. BallController 미러링:
    //  - 순수 DI 생성자, OnInitialize에서 IClock 구독.
    //  - id 키 병렬 딕셔너리(model/view/handle) 명단 소유.
    //  - 그리드 배치 권한은 GridController에 위임(블록=occupant=IDamageable=EnemyView).
    //  - 등장 연출은 EnemyEntranceChoreographer, 하강/쌓임은 EnemyDescentSimulator 소유 — 여기는 명단+데미지+이벤트만.
    //  - 데미지는 EnemyView가 여기로 포워드 → HP감산(모델)·사망 시 RaiseKill·디스폰·그리드 해제.
    public sealed class EnemyController : BaseController
    {
        private readonly IEnemyFactory _factory;
        private readonly IClock _clock;
        private readonly GridController _grid;
        private readonly CombatEventHub _hub;
        private readonly DamageResolver _resolver;

        private readonly Dictionary<string, EnemyModel> _models = new();
        private readonly Dictionary<string, EnemyView> _views = new();
        private readonly Dictionary<string, int> _handles = new();
        private readonly List<string> _idCache = new();

        private readonly EnemyEntranceChoreographer _entrance;
        private readonly EnemyDescentSimulator _descent;
        private readonly BreachChoreographer _breach;
        private readonly EnemyStatusSimulator _status;
        private Vector2 _defensePoint = new Vector2(0f, -6.70f); // 침범 연출 돌진 목표(캐릭터). GameLifetimeScope가 주입.

        // Last Match(패시브, §212): 킬 시 반경 폭발. SkillRuntime이 로드아웃 변경 시 SetLastMatch로 값 주입(0=미보유).
        private const int MaxExplosionDepth = 4; // 체인 허용하되 무한 재귀 방지(§257 depth 가드)
        private float _lastMatchDamage;
        private float _lastMatchRadius;
        private int _explosionDepth;
        private readonly List<int>[] _explosionHits; // depth별 재사용 버퍼(체인 재귀 안전 + 킬버스트 할당 방지)

        public EnemyController(IEnemyFactory factory, IClock clock, GridController grid, CombatEventHub hub, DamageResolver resolver)
        {
            _factory = factory;
            _clock = clock;
            _grid = grid;
            _hub = hub;
            _resolver = resolver;
            _entrance = new EnemyEntranceChoreographer();
            _descent = new EnemyDescentSimulator(grid, _models, _views, _handles, OnDescentBreach);
            _breach = new BreachChoreographer(OnBreachImpact);
            _status = new EnemyStatusSimulator(ApplyBurnDamage, SetFreezeSlow, SetEnemyBurning, SetEnemyFrozen);
            _explosionHits = new List<int>[MaxExplosionDepth];
            for (int i = 0; i < MaxExplosionDepth; i++) _explosionHits[i] = new List<int>();
        }

        protected override void OnInitialize() => _clock.OnFixedTick += OnClockFixedTick;

        protected override void OnDispose()
        {
            _clock.OnFixedTick -= OnClockFixedTick;
            DespawnAll();
        }

        protected override void OnReset() => DespawnAll();
        protected override void OnTick(float deltaTime) { }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        // ---- 스폰 ----
        // 지정 셀 앵커에 배치(손배치 웨이브). cascadeIndex = 등장 순서(낙하 지연 계산). 자리 없으면 null.
        // 스폰 즉시 셀은 그리드에 예약되지만, 몹은 등장 연출이 끝날 때까지 무적(콜라이더 off).
        public EnemyModel Spawn(EnemyDefinition definition, CellCoord anchor, int cascadeIndex, float hpScale = 1f)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return null;
            Footprint fp = definition.Footprint;
            if (!_grid.Model.CanPlace(anchor, fp)) return null;

            Vector2 center = _grid.Model.FootprintWorldCenter(anchor, fp);
            string id = _factory.GenerateId();
            (EnemyModel model, EnemyView view) = _factory.Create(id, definition, center, hpScale);
            if (model == null || view == null) return null;

            // occupant=view 로 배치(볼 콜라이더가 같은 IDamageable를 가리킴).
            if (!_grid.TryPlaceBlock(anchor, fp, out BlockPlacement placement, view))
            {
                _factory.Release(model, view);
                return null;
            }

            view.SetFootprintSize(placement.WorldSize);
            view.SetDamageSink(HandleDamage);
            view.SetBurnSink(HandleBurn);
            view.SetFreezeSink(HandleFreeze);

            _models[id] = model;
            _views[id] = view;
            _handles[id] = placement.Handle;

            // 등장 연출 시작: 낙하 캐스케이드(순서별 지연) → 음영 → 낙하 → 덜컹 → 활성.
            _entrance.Begin(id, model, view, cascadeIndex, center, placement.WorldSize);
            return model;
        }

        // 스테이지가 서브그룹 스폰 게이트로 사용: 이 앵커에 지금 배치 가능한가(상단행이 비었는가).
        public bool CanSpawnAt(EnemyDefinition definition, CellCoord anchor)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return false;
            return _grid.Model.CanPlace(anchor, definition.Footprint);
        }

        // Last Match 파라미터 주입(SkillRuntime). damage 또는 radius가 0이면 미보유(폭발 비활성).
        public void SetLastMatch(float damage, float radius)
        {
            _lastMatchDamage = damage;
            _lastMatchRadius = radius;
        }

        // ---- 데미지(EnemyView가 포워드) ----
        public void HandleDamage(EnemyView view, int amount)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return; // 등장 중(낙하)엔 무적

            model.TakeDamage(amount); // HP 감산 + Raise(숫자 갱신). 사망 판정도 여기서.
            if (!model.IsDead) return;

            Vector2 deathPos = model.Position; // Last Match 폭발 중심(Despawn 전 캡처)
            // 인접 판정용 격자 배치도 Despawn 전 캡처 — Despawn하면 handle/placement가 사라짐.
            BlockPlacement deadPlacement = default;
            bool hasPlacement = _handles.TryGetValue(model.Id, out int deadHandle)
                && _grid.TryGetPlacement(deadHandle, out deadPlacement);
            _hub?.RaiseKill();
            _hub?.RaiseEnemyDeath(deathPos); // 돌 블럭 깨짐 연출(모든 킬 경로 공통 — 여기가 유일한 사망 지점)
            Despawn(model.Id);
            // 킬(직격·번·행뎀·분열·체인 무관) → 죽은 적 풋프린트의 8방향 인접칸 적들에게 폭발뎀(보유 시).
            if (hasPlacement) TryLastMatchExplosion(deadPlacement.Anchor, deadPlacement.Footprint, deathPos);
        }

        // Last Match: 죽은 적 풋프린트의 8방향 인접칸(대각 포함, 1칸 두께 링)을 격자에서 훑어, 점유한 적들에게 flat 2차뎀(무크리).
        // 유클리드 반경 아님 = 격자 인접(레벨은 데미지만↑, 범위 고정 3x3). 큰 적은 풋프린트 링이라 중심점 근사 없음.
        // 중복 셀은 적당 1회(2x2 적은 한 번만). 폭발 처치가 또 폭발을 불러 depth 가드(§257). hits는 지역이라 재귀 안전.
        private void TryLastMatchExplosion(CellCoord anchor, Footprint fp, Vector2 center)
        {
            if (_lastMatchDamage <= 0f || _resolver == null) return; // radius는 이제 범위 판정에 안 씀(장착 신호는 damage>0)
            if (_explosionDepth >= MaxExplosionDepth) return;
            _explosionDepth++;

            _hub?.RaiseExplosion(center, _lastMatchRadius); // 붉은 폭발 연출(폭발 발생 지점마다 1회 · 체인이면 각 center)

            // 풋프린트를 둘러싼 1칸 링 순회 → 점유 적 핸들 수집(중복 제거). depth별 재사용 버퍼(체인 재귀 안전).
            List<int> hits = _explosionHits[_explosionDepth - 1];
            hits.Clear();
            int c0 = anchor.Col, r0 = anchor.Row;
            int w = Mathf.Max(1, fp.Width), h = Mathf.Max(1, fp.Height);
            for (int c = c0 - 1; c <= c0 + w; c++)
            for (int r = r0 - 1; r <= r0 + h; r++)
            {
                if (c >= c0 && c < c0 + w && r >= r0 && r < r0 + h) continue; // 풋프린트 내부(자기 자리) 제외
                if (c < 0 || c >= _grid.Cols || r < 0 || r >= _grid.Rows) continue; // 격자 밖
                int occ = _grid.OccupantHandleAt(c, r);
                if (occ != GridMap.Empty && !hits.Contains(occ)) hits.Add(occ);
            }

            for (int i = 0; i < hits.Count; i++)
            {
                if (!_grid.TryGetOccupant(hits[i], out IDamageable dmg) || !(dmg is EnemyView v) || v.Model == null) continue;
                if (v.Model.IsDead || v.Model.IsEntering) continue; // 등장 중(무적)·사망 제외
                Vector2 pos = v.Model.Position;
                HitContext ctx = HitContext.Secondary(v, BallSourceType.Normal, DamageKind.Explosion, _lastMatchDamage);
                _resolver.Resolve(ctx); // 사망 시 HandleDamage→RaiseKill→TryLastMatchExplosion 재귀(depth 가드, hits 지역이라 안전)
                if (ctx.FinalDamage > 0) _hub?.RaiseHit(v, pos, ctx.FinalDamage, false, Vector2.zero, BallSourceType.Normal, ctx.Kind); // 폭발 = 흰색·무방향(반동·임팩트 없음)
            }

            _explosionDepth--;
        }

        // 볼 모듈(Fire) → EnemyView.ApplyBurn → 여기. 상태 시뮬레이터에 번 부여(독립타이머 스택·캡).
        private void HandleBurn(EnemyView view, float duration, float dps, int maxStacks)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return;
            _status.ApplyBurn(model.Id, duration, dps, maxStacks);
        }

        // 볼 모듈(Ice) → EnemyView.ApplyFreeze → 여기. 상태 시뮬레이터에 냉동 부여(무스택 refresh).
        private void HandleFreeze(EnemyView view, float duration, float slow)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return;
            _status.ApplyFreeze(model.Id, duration, slow);
        }

        // 상태 시뮬레이터 → 여기: 현재 냉동 슬로우를 모델 하강속도에 반영(매 틱, 무변경이면 모델이 조기반환). 만료 시 0.
        private void SetFreezeSlow(string id, float slow)
        {
            if (_models.TryGetValue(id, out EnemyModel model) && model != null) model.SetFreezeSlow(slow);
        }

        // 번 시각 상태(EnemyStatusSimulator) → 뷰의 불꽃 VFX on/off. 첫 스택=on, 전 스택 만료/디스폰=off.
        private void SetEnemyBurning(string id, bool on)
        {
            if (_views.TryGetValue(id, out EnemyView view) && view != null) view.SetBurning(on);
        }

        // 냉동 시각 상태(EnemyStatusSimulator) → 뷰의 서리 VFX on/off. 첫 냉동=on, 전 냉동 만료/디스폰=off.
        private void SetEnemyFrozen(string id, bool on)
        {
            if (_views.TryGetValue(id, out EnemyView view) && view != null) view.SetFrozen(on);
        }

        // 번 초당 틱: 2차 데미지(flat·무크리·무버프)를 동일 DamageResolver로 적용 → 숫자표기·사망 이벤트 통일.
        // Resolve → view.ApplyDamage → HandleDamage 경로라 사망 시 RaiseKill·디스폰(상태도 정리)이 그대로 발동.
        private void ApplyBurnDamage(string id, float dps)
        {
            if (_resolver == null || !_views.TryGetValue(id, out EnemyView view) || view.Model == null) return;
            Vector2 pos = view.Model.Position;                 // Resolve 전 캡처(살상 번틱 디스폰 대비)
            HitContext ctx = HitContext.Secondary(view, BallSourceType.Fire, DamageKind.Burn, dps);
            _resolver.Resolve(ctx);
            if (ctx.FinalDamage > 0) _hub?.RaiseHit(view, pos, ctx.FinalDamage, ctx.IsCrit, Vector2.zero, BallSourceType.Fire, ctx.Kind); // 번 = 흰색·무방향(반동·임팩트 없음)
        }

        // ---- IClock 틱: 등장 연출(입장 중) → 연속 하강(입장 완료) ----
        protected override void OnFixedTick(float fixedDeltaTime)
        {
            _entrance.Tick(fixedDeltaTime);
            _descent.Tick(fixedDeltaTime);
            _breach.Tick(fixedDeltaTime);  // 침범 연출(부들부들→돌진→충격)
            _status.Tick(fixedDeltaTime); // 번 감쇠 + 초당 데미지(사망 시 디스폰이 상태도 정리)
        }

        // 방어선 도달 → 즉시 침범 대신 침범 연출 시작(부들부들→돌진→충격). 격자 셀 해제(위 적 진행 허용).
        private void OnDescentBreach(string id, int breachDamage)
        {
            if (_handles.TryGetValue(id, out int h)) { _grid.RemoveBlock(h); _handles.Remove(id); }
            if (_models.TryGetValue(id, out EnemyModel m) && _views.TryGetValue(id, out EnemyView v))
            {
                m.BeginBreaching();
                v.SetColliderEnabled(false); // 달려들 때 볼 적중 차단(관통) — Daniel
                _breach.Begin(id, m, v, breachDamage, _defensePoint);
            }
            else Despawn(id);
        }

        // 침범 연출 완료(캐릭터 충격) → 붉은 플로팅 숫자 + 피 연출 + 베이스 HP 감소 + 디스폰.
        private void OnBreachImpact(string id, int breachDamage, Vector2 impactPos)
        {
            _hub?.RaiseHit(null, impactPos, breachDamage, true, Vector2.zero, BallSourceType.Normal, Game.Combat.DamageKind.Direct); // 캐릭터 피격 = 붉은 숫자(view null → 텍스트만)
            _hub?.RaiseBaseHit(impactPos);   // 피 파티클(config 배선 시)
            _hub?.RaiseBreach(breachDamage); // 베이스 HP 감소(+사망 시 실패 시퀀스)
            Despawn(id);
        }

        // 캐릭터(방어선) 월드 좌표 주입 — 침범 연출 돌진 목표.
        public void SetDefensePoint(Vector2 point) => _defensePoint = point;

        // ---- 디스폰 ----
        public void Despawn(string id)
        {
            if (!_models.TryGetValue(id, out EnemyModel model)) return;
            _views.TryGetValue(id, out EnemyView view);
            if (_handles.TryGetValue(id, out int handle)) _grid.RemoveBlock(handle);
            _entrance.Remove(id);
            _breach.Remove(id);
            _status.Remove(id);
            _factory.Release(model, view);
            _models.Remove(id);
            _views.Remove(id);
            _handles.Remove(id);
        }

        public void DespawnAll()
        {
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--) Despawn(_idCache[i]);
        }
    }
}
