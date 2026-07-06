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
        private readonly EnemyStatusSimulator _status;

        // Last Match(패시브, §212): 킬 시 반경 폭발. SkillRuntime이 로드아웃 변경 시 SetLastMatch로 값 주입(0=미보유).
        private const int MaxExplosionDepth = 4; // 체인 허용하되 무한 재귀 방지(§257 depth 가드)
        private float _lastMatchDamage;
        private float _lastMatchRadius;
        private int _explosionDepth;
        private readonly List<string> _explodeCache = new();

        public EnemyController(IEnemyFactory factory, IClock clock, GridController grid, CombatEventHub hub, DamageResolver resolver)
        {
            _factory = factory;
            _clock = clock;
            _grid = grid;
            _hub = hub;
            _resolver = resolver;
            _entrance = new EnemyEntranceChoreographer();
            _descent = new EnemyDescentSimulator(grid, _models, _views, _handles, OnDescentBreach);
            _status = new EnemyStatusSimulator(ApplyBurnDamage, SetFreezeSlow, SetEnemyBurning);
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
        public EnemyModel Spawn(EnemyDefinition definition, CellCoord anchor, int cascadeIndex)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return null;
            Footprint fp = definition.Footprint;
            if (!_grid.Model.CanPlace(anchor, fp)) return null;

            Vector2 center = _grid.Model.FootprintWorldCenter(anchor, fp);
            string id = _factory.GenerateId();
            (EnemyModel model, EnemyView view) = _factory.Create(id, definition, center);
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
            _hub?.RaiseKill();
            Despawn(model.Id);
            TryLastMatchExplosion(deathPos); // 킬(직격·번·행뎀·분열·체인 무관) → 반경 폭발(보유 시)
        }

        // Last Match: 킬 위치 반경 안 적들에게 flat 2차뎀(무크리). 폭발 처치가 또 폭발을 부를 수 있어 depth 가드(§257).
        // AoE 처치도 동일 HandleDamage 경유라 자연히 이어짐(§255). 사망한 적은 이미 Despawn돼 반경 집합에서 제외됨.
        private void TryLastMatchExplosion(Vector2 center)
        {
            if (_lastMatchDamage <= 0f || _lastMatchRadius <= 0f || _resolver == null) return;
            if (_explosionDepth >= MaxExplosionDepth) return;
            _explosionDepth++;

            float r2 = _lastMatchRadius * _lastMatchRadius;
            _explodeCache.Clear();
            foreach (KeyValuePair<string, EnemyModel> kv in _models)
            {
                EnemyModel m = kv.Value;
                if (m == null || m.IsDead || m.IsEntering) continue; // 등장 중(무적)·사망 제외
                if ((m.Position - center).sqrMagnitude <= r2) _explodeCache.Add(kv.Key);
            }

            for (int i = 0; i < _explodeCache.Count; i++)
            {
                if (!_views.TryGetValue(_explodeCache[i], out EnemyView v) || v.Model == null) continue; // 체인 중 이미 디스폰
                Vector2 pos = v.Model.Position;
                HitContext ctx = HitContext.Secondary(v, BallSourceType.Normal, DamageKind.Explosion, _lastMatchDamage);
                _resolver.Resolve(ctx); // 사망 시 HandleDamage→RaiseKill→TryLastMatchExplosion 재귀(depth 가드)
                if (ctx.FinalDamage > 0) _hub?.RaiseHit(v, pos, ctx.FinalDamage, false, Vector2.zero, BallSourceType.Normal); // 폭발 = 흰색·무방향(반동·임팩트 없음)
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

        // 번 초당 틱: 2차 데미지(flat·무크리·무버프)를 동일 DamageResolver로 적용 → 숫자표기·사망 이벤트 통일.
        // Resolve → view.ApplyDamage → HandleDamage 경로라 사망 시 RaiseKill·디스폰(상태도 정리)이 그대로 발동.
        private void ApplyBurnDamage(string id, float dps)
        {
            if (_resolver == null || !_views.TryGetValue(id, out EnemyView view) || view.Model == null) return;
            Vector2 pos = view.Model.Position;                 // Resolve 전 캡처(살상 번틱 디스폰 대비)
            HitContext ctx = HitContext.Secondary(view, BallSourceType.Fire, DamageKind.Burn, dps);
            _resolver.Resolve(ctx);
            if (ctx.FinalDamage > 0) _hub?.RaiseHit(view, pos, ctx.FinalDamage, ctx.IsCrit, Vector2.zero, BallSourceType.Fire); // 번 = 흰색·무방향(반동·임팩트 없음)
        }

        // ---- IClock 틱: 등장 연출(입장 중) → 연속 하강(입장 완료) ----
        protected override void OnFixedTick(float fixedDeltaTime)
        {
            _entrance.Tick(fixedDeltaTime);
            _descent.Tick(fixedDeltaTime);
            _status.Tick(fixedDeltaTime); // 번 감쇠 + 초당 데미지(사망 시 디스폰이 상태도 정리)
        }

        // 하강 시뮬레이터가 방어선 침범을 보고 → 이벤트 발화(StageController가 베이스 HP 감소) 후 디스폰.
        private void OnDescentBreach(string id, int breachDamage)
        {
            _hub?.RaiseBreach(breachDamage);
            Despawn(id);
        }

        // ---- 디스폰 ----
        public void Despawn(string id)
        {
            if (!_models.TryGetValue(id, out EnemyModel model)) return;
            _views.TryGetValue(id, out EnemyView view);
            if (_handles.TryGetValue(id, out int handle)) _grid.RemoveBlock(handle);
            _entrance.Remove(id);
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
