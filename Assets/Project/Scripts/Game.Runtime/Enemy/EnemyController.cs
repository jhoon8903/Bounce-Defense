using System.Collections.Generic;
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

        private readonly Dictionary<string, EnemyModel> _models = new();
        private readonly Dictionary<string, EnemyView> _views = new();
        private readonly Dictionary<string, int> _handles = new();
        private readonly List<string> _idCache = new();

        private readonly EnemyEntranceChoreographer _entrance;
        private readonly EnemyDescentSimulator _descent;

        public EnemyController(IEnemyFactory factory, IClock clock, GridController grid, CombatEventHub hub)
        {
            _factory = factory;
            _clock = clock;
            _grid = grid;
            _hub = hub;
            _entrance = new EnemyEntranceChoreographer();
            _descent = new EnemyDescentSimulator(grid, _models, _views, _handles, OnDescentBreach);
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

            _models[id] = model;
            _views[id] = view;
            _handles[id] = placement.Handle;

            // 등장 연출 시작: 낙하 캐스케이드(순서별 지연) → 음영 → 낙하 → 덜컹 → 활성.
            _entrance.Begin(id, model, view, cascadeIndex, center, placement.WorldSize);
            return model;
        }

        // ---- 데미지(EnemyView가 포워드) ----
        public void HandleDamage(EnemyView view, int amount)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return; // 등장 중(낙하)엔 무적

            model.TakeDamage(amount); // HP 감산 + Raise(숫자 갱신). 사망 판정도 여기서.
            if (!model.IsDead) return;

            _hub?.RaiseKill();
            Despawn(model.Id);
        }

        // ---- IClock 틱: 등장 연출(입장 중) → 연속 하강(입장 완료) ----
        protected override void OnFixedTick(float fixedDeltaTime)
        {
            _entrance.Tick(fixedDeltaTime);
            _descent.Tick(fixedDeltaTime);
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
