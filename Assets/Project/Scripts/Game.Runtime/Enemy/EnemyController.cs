using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 lifecycle 권한. BallController + GridController 미러링:
    //  - 순수 DI 생성자, OnInitialize에서 IClock 구독.
    //  - id 키 병렬 딕셔너리(model/view/handle) + 스냅샷 순회.
    //  - 그리드 배치 권한은 GridController에 위임(블록=occupant=IDamageable=EnemyView).
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

        public int ActiveCount => _models.Count;

        public EnemyController(IEnemyFactory factory, IClock clock, GridController grid, CombatEventHub hub)
        {
            _factory = factory;
            _clock = clock;
            _grid = grid;
            _hub = hub;
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
        // col < 0 = 상단 우선 빈 자리 자동. col >= 0 = 해당 열 최상단 행(0)에 배치.
        // 반환 null = 자리 없음/풀 고갈.
        public EnemyModel Spawn(EnemyDefinition definition, int col = -1)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return null;
            Footprint fp = definition.Footprint;

            CellCoord anchor;
            if (col >= 0)
            {
                anchor = new CellCoord(col, 0);
                if (!_grid.Model.CanPlace(anchor, fp)) return null;
            }
            else if (!_grid.Model.TryFindFreeAnchor(fp, out anchor))
            {
                return null;
            }

            Vector2 center = _grid.Model.FootprintWorldCenter(anchor, fp);
            string id = _factory.GenerateId();
            (EnemyModel model, EnemyView view) = _factory.Create(id, definition, center);
            if (model == null || view == null) return null;

            // occupant=view 로 배치(레이저 행 조회 + 볼 콜라이더가 같은 IDamageable를 가리킴).
            if (!_grid.TryPlaceBlock(anchor, fp, out BlockPlacement placement, view))
            {
                _factory.Release(model, view);
                return null;
            }

            view.SetFootprintSize(placement.WorldSize);
            view.SetDamageSink(HandleDamage);
            model.SetGridHandle(placement.Handle);

            _models[id] = model;
            _views[id] = view;
            _handles[id] = placement.Handle;
            return model;
        }

        // ---- 데미지(EnemyView가 포워드) ----
        public void HandleDamage(EnemyView view, int amount, HitContext context)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead) return;

            model.TakeDamage(amount); // HP 감산 + Raise(숫자 갱신). 사망 판정도 여기서.
            if (!model.IsDead) return;

            _hub?.RaiseKill(new EnemyKillInfo(view.transform.position, context));
            Despawn(model.Id);
        }

        // ---- 연속 하강(IClock) ----
        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_models.Count == 0) return;
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            // 하단(행 인덱스 큰=화면 아래) 적부터 처리 → 아래 적이 먼저 내려가 칸을 비우면 위 적이 같은 틱에 이어 내려갈 수 있다.
            _idCache.Sort(CompareByRowDescending);
            float cellSize = _grid.CellSize;

            for (int i = 0; i < _idCache.Count; i++)
            {
                string id = _idCache[i];
                if (!_models.TryGetValue(id, out EnemyModel model)) continue;
                DescendOne(id, model, fixedDeltaTime, cellSize);
            }
        }

        private int CompareByRowDescending(string a, string b) => RowOf(b).CompareTo(RowOf(a));

        private int RowOf(string id)
        {
            if (_handles.TryGetValue(id, out int h) && _grid.TryGetPlacement(h, out BlockPlacement p)) return p.Anchor.Row;
            return -1;
        }

        // 한 적의 1틱 하강. 현재 행 중심 아래로 내려가려면 '다음 행'이 비어야 한다:
        //  - 다음 행 빔  → 부드럽게 하강, 다음 행 중심 도달 시 그리드 재등록(레이저/배치 정합).
        //  - 막힘(점유) → 현재 행 중심에 flush로 정지. 절대 오버슛/위로-스냅 안 함(스택은 아래 적 위에 딱 붙어 쉼).
        //  - 보드 하단  → 방어선 도달 → 디스폰(Phase 2-2: 베이스 HP 감소).
        private void DescendOne(string id, EnemyModel model, float dt, float cellSize)
        {
            float speed = model.DescentSpeed;
            if (speed <= 0f) return;
            if (!_handles.TryGetValue(id, out int handle)) return;
            if (!_grid.TryGetPlacement(handle, out BlockPlacement placement)) return;
            if (!_views.TryGetValue(id, out EnemyView view)) return;

            Footprint fp = placement.Footprint;
            float centerY = placement.WorldCenter.y;
            float x = model.Position.x;
            float desiredY = model.Position.y - speed * dt;

            // 아직 현재 행 중심 위/at → 점유 걱정 없이 부드럽게 하강.
            if (desiredY >= centerY)
            {
                model.SetPosition(new Vector2(x, desiredY));
                return;
            }

            // 현재 행 중심 아래로 가려 함 → 다음 행이 비어야 진행. ignoreHandle=self로 핸들 churn 없이 조회.
            CellCoord nextAnchor = new CellCoord(placement.Anchor.Col, placement.Anchor.Row + 1);
            bool offBottom = nextAnchor.Row + fp.Height > _grid.Rows;
            bool canDescend = !offBottom && _grid.Model.CanPlace(nextAnchor, fp, handle);

            if (!canDescend)
            {
                // 막힘: 현재 행 중심에 flush 정지(오버슛/스냅 없음).
                model.SetPosition(new Vector2(x, centerY));
                if (offBottom)
                {
                    // 방어선 도달(다음 행이 보드 밖) → 베이스 침범 이벤트 발화 후 디스폰(StageController가 베이스 HP 감소).
                    int breach = model.Definition != null ? model.Definition.BreachDamage : 0;
                    _hub?.RaiseBreach(new EnemyBreachInfo(breach, model.Position));
                    Despawn(id);
                }
                return;
            }

            // 아래 비었음 → 하강. 다음 행 중심 도달 시 그리드 재등록.
            model.SetPosition(new Vector2(x, desiredY));
            if (desiredY <= centerY - cellSize)
            {
                _grid.RemoveBlock(handle);
                if (_grid.TryPlaceBlock(nextAnchor, fp, out BlockPlacement moved, view))
                {
                    model.SetGridHandle(moved.Handle);
                    _handles[id] = moved.Handle;
                }
                else if (_grid.TryPlaceBlock(placement.Anchor, fp, out BlockPlacement restored, view))
                {
                    // 이례적(방금 비었는데 실패) → 원위치 복구(스냅 없음, 이미 desiredY에 있음).
                    model.SetGridHandle(restored.Handle);
                    _handles[id] = restored.Handle;
                }
            }
        }

        // ---- 디스폰 ----
        public void Despawn(string id)
        {
            if (!_models.TryGetValue(id, out EnemyModel model)) return;
            _views.TryGetValue(id, out EnemyView view);
            if (_handles.TryGetValue(id, out int handle)) _grid.RemoveBlock(handle);
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
