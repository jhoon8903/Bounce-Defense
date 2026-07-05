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

        // 등장 연출 튜닝 상수(코드 고정). 인스펙터 튜닝이 필요해지면 SO로 승격 + DI 주입.
        private const float CascadeStagger = 0.1f;  // 배치 순서 간 등장 지연(초) = 우루루루 캐스케이드
        private const float ShadowLead = 0.12f;     // 낙하 전 음영 선행 시간
        private const float DropDuration = 0.28f;   // 낙하 시간
        private const float DropHeight = 4f;        // 착지셀 위 시작 높이(월드 유닛)
        private const float BounceDuration = 0.14f; // 덜컹(스쿼시) 시간
        private const float BounceScale = 0.22f;    // 스쿼시 세기
        private const float ShadowAlpha = 0.35f;    // 음영 투명도

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
        // 지정 셀 앵커에 배치(손배치 웨이브). cascadeIndex = 등장 순서(낙하 지연 계산). 자리 없으면 null.
        // 스폰 즉시 셀은 그리드에 예약되지만, 몹은 위에서 낙하 후 착지할 때까지 무적(콜라이더 off).
        public EnemyModel Spawn(EnemyDefinition definition, CellCoord anchor, int cascadeIndex)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return null;
            Footprint fp = definition.Footprint;
            if (!_grid.Model.CanPlace(anchor, fp)) return null;

            Vector2 center = _grid.Model.FootprintWorldCenter(anchor, fp);
            float dropFromY = center.y + DropHeight;

            string id = _factory.GenerateId();
            (EnemyModel model, EnemyView view) = _factory.Create(id, definition, new Vector2(center.x, dropFromY));
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

            // 등장 연출 시작: 낙하 캐스케이드(순서별 지연) → 음영 → 낙하 → 덜컹 → 활성.
            float delay = Mathf.Max(0, cascadeIndex) * CascadeStagger;
            model.BeginEntrance(delay, ShadowLead, DropDuration, BounceDuration, dropFromY, center.y);
            view.BeginEntranceVisual(null, placement.WorldSize);
            view.SetEntranceFrame(false, 0f, center, 0f);

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
            if (model == null || model.IsDead || model.IsEntering) return; // 등장 중(낙하)엔 무적

            model.TakeDamage(amount); // HP 감산 + Raise(숫자 갱신). 사망 판정도 여기서.
            if (!model.IsDead) return;

            _hub?.RaiseKill(new EnemyKillInfo(view.transform.position, context));
            Despawn(model.Id);
        }

        // ---- IClock 틱: 등장 연출(입장 중) + 연속 하강(입장 완료) ----
        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_models.Count == 0) return;

            // 1) 등장 연출 진행. 입장 중인 적은 하강에서 제외된다.
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = 0; i < _idCache.Count; i++)
            {
                string id = _idCache[i];
                if (_models.TryGetValue(id, out EnemyModel m) && m.IsEntering)
                    TickEntrance(id, m, fixedDeltaTime);
            }

            // 2) 하강(입장 완료된 적만). 하단(행 인덱스 큰=화면 아래) 우선 → 아래가 먼저 비우면 위가 같은 틱에 이어 내려감.
            _idCache.Clear();
            foreach (KeyValuePair<string, EnemyModel> kv in _models)
                if (!kv.Value.IsEntering) _idCache.Add(kv.Key);
            if (_idCache.Count == 0) return;
            _idCache.Sort(CompareByRowDescending);
            float cellSize = _grid.CellSize;

            for (int i = 0; i < _idCache.Count; i++)
            {
                string id = _idCache[i];
                if (!_models.TryGetValue(id, out EnemyModel model) || model.IsEntering) continue;
                DescendOne(id, model, fixedDeltaTime, cellSize);
            }
        }

        // 한 적의 등장 연출 1틱. 셀은 이미 그리드에 예약됨. 착지 완료 시 활성(콜라이더 on)→다음 틱부터 하강.
        private void TickEntrance(string id, EnemyModel model, float dt)
        {
            if (!_views.TryGetValue(id, out EnemyView view)) return;
            float elapsed = model.AdvanceEntrance(dt);

            float delay = model.EntDelay;
            float dropStart = delay + model.EntShadowLead;
            float landTime = dropStart + model.EntDropDuration;
            float endTime = landTime + model.EntBounceDuration;

            float x = model.Position.x;
            float landedY = model.LandedY;
            Vector2 shadowPos = new Vector2(x, landedY);

            if (elapsed >= endTime)
            {
                // 등장 완료 → 활성. 착지 셀에 정합(하강 시작점 = 셀 중심).
                model.SetPosition(new Vector2(x, landedY));
                view.EndEntranceVisual();
                model.MarkActive();
                return;
            }
            if (elapsed < delay)
            {
                view.SetEntranceFrame(false, 0f, shadowPos, 0f); // 대기: 전부 숨김
                return;
            }
            if (elapsed < dropStart)
            {
                // 음영 페이드인(몸체 숨김) — 착지 지점 텔레그래프.
                float a = Mathf.InverseLerp(delay, dropStart, elapsed) * ShadowAlpha;
                view.SetEntranceFrame(false, a, shadowPos, 0f);
                return;
            }
            if (elapsed < landTime)
            {
                // 낙하(ease-in, t² 가속) — 위에서 착지 셀로.
                float t = Mathf.InverseLerp(dropStart, landTime, elapsed);
                float y = Mathf.Lerp(model.DropFromY, landedY, t * t);
                model.SetPosition(new Vector2(x, y));
                view.SetEntranceFrame(true, ShadowAlpha, shadowPos, 0f);
                return;
            }
            // 착지 후 덜컹(스쿼시 감쇠) + 음영 페이드아웃.
            model.SetPosition(new Vector2(x, landedY));
            float bt = Mathf.InverseLerp(landTime, endTime, elapsed);
            view.SetEntranceFrame(true, ShadowAlpha * (1f - bt), shadowPos, BounceScale * (1f - bt));
        }

        private int CompareByRowDescending(string a, string b) => RowOf(b).CompareTo(RowOf(a));

        private int RowOf(string id)
        {
            if (_handles.TryGetValue(id, out int h) && _grid.TryGetPlacement(h, out BlockPlacement p)) return p.Anchor.Row;
            return -1;
        }

        // 한 적의 1틱 하강. 이산 '칸 비었나' 게이트가 아니라 연속 '아래 이웃과 안전거리' 로 판정 → 스터터 없음.
        //  - 아래 이웃 있음 → 그 적의 실제 위치 기준 한 칸 위까지만 내려가 얹힘(같은 속도면 대열째 쭉, 빠른 적은 위에 얹혀 감속).
        //  - 아래 비었음   → 방어선(바닥)까지 자유 하강. 바닥 도달 시 침범 이벤트 + 디스폰.
        //  - 그리드 재등록(레이저/배치 정합)은 셀 경계 넘을 때 유지하되, 움직임 자체는 위치 기반.
        private void DescendOne(string id, EnemyModel model, float dt, float cellSize)
        {
            float speed = model.DescentSpeed;
            if (speed <= 0f) return;
            if (!_handles.TryGetValue(id, out int handle)) return;
            if (!_grid.TryGetPlacement(handle, out BlockPlacement placement)) return;
            if (!_views.TryGetValue(id, out EnemyView view)) return;

            Footprint fp = placement.Footprint;
            float x = model.Position.x;
            float desiredY = model.Position.y - speed * dt;

            // 아래 이웃(같은 열에서 가장 가까운 적)이 있으면 그 위에 얹혀 연속 추종.
            if (TryComputeBelowFloor(handle, placement, fp, cellSize, out float belowFloor))
            {
                float clampedY = Mathf.Max(desiredY, belowFloor); // 이웃 위로 파고들지 않음
                if (clampedY > model.Position.y) clampedY = model.Position.y; // 위로 스냅 금지
                model.SetPosition(new Vector2(x, clampedY));
                MaybeReRegister(id, model, placement, fp, view, handle, cellSize);
                return;
            }

            // 아래 비었음 → 방어선(바닥 유효 행)까지 자유 하강.
            float defenseFloorY = _grid.Model.FootprintWorldCenter(
                new CellCoord(placement.Anchor.Col, _grid.Rows - fp.Height), fp).y;
            if (desiredY < defenseFloorY)
            {
                // 방어선 도달 → 베이스 침범 이벤트 발화 후 디스폰(StageController가 베이스 HP 감소).
                model.SetPosition(new Vector2(x, defenseFloorY));
                int breach = model.Definition != null ? model.Definition.BreachDamage : 0;
                _hub?.RaiseBreach(new EnemyBreachInfo(breach, model.Position));
                Despawn(id);
                return;
            }
            model.SetPosition(new Vector2(x, desiredY));
            MaybeReRegister(id, model, placement, fp, view, handle, cellSize);
        }

        // 같은 열(풋프린트가 걸친 모든 열)에서 바로 아래의 가장 가까운 적을 찾아, 겹치지 않는 최소 중심 Y(floor)를 낸다.
        // floor = 아래적중심 + (내높이/2 + 아래높이/2)*cell. 아래 이웃 없으면 false. (입장 중 이웃은 착지 셀 기준.)
        private bool TryComputeBelowFloor(int selfHandle, BlockPlacement p, Footprint fp, float cellSize, out float floorY)
        {
            floorY = float.NegativeInfinity;
            bool found = false;
            int bottomRow = p.Anchor.Row + fp.Height - 1;
            int rows = _grid.Rows;
            for (int col = p.Anchor.Col; col < p.Anchor.Col + fp.Width; col++)
            {
                for (int row = bottomRow + 1; row < rows; row++)
                {
                    int h = _grid.OccupantHandleAt(col, row);
                    if (h == GridMap.Empty || h == selfHandle) continue;
                    if (_grid.TryGetOccupant(h, out IDamageable occ) && occ is EnemyView ev && ev.Model != null)
                    {
                        EnemyModel bm = ev.Model;
                        float belowY = bm.IsEntering ? bm.LandedY : bm.Position.y; // 입장 중이면 착지 셀 중심(안정적)
                        float allowed = belowY + (fp.Height + bm.Footprint.Height) * cellSize * 0.5f;
                        if (allowed > floorY) floorY = allowed;
                        found = true;
                    }
                    break; // 이 열에서 가장 가까운(위쪽) 이웃만
                }
            }
            return found;
        }

        // 중심이 다음 행 중심 이하로 내려갔고 그 행이 비었으면 그리드 셀을 한 행 아래로 재등록(레이저/배치 정합).
        // 다음 셀이 아직 점유면 보류(연속 clamp가 이미 위치를 막고 있으므로 데드핸들/위로-스냅 없음).
        private void MaybeReRegister(string id, EnemyModel model, BlockPlacement placement, Footprint fp, EnemyView view, int handle, float cellSize)
        {
            float centerY = placement.WorldCenter.y;
            if (model.Position.y > centerY - cellSize) return; // 아직 다음 행 중심까지 안 내려감
            CellCoord nextAnchor = new CellCoord(placement.Anchor.Col, placement.Anchor.Row + 1);
            if (nextAnchor.Row + fp.Height > _grid.Rows) return;      // 바닥 밖 → 재등록 안 함(방어선은 DescendOne이 처리)
            if (!_grid.Model.CanPlace(nextAnchor, fp, handle)) return; // 다음 셀 아직 점유 → 보류
            _grid.RemoveBlock(handle);
            if (_grid.TryPlaceBlock(nextAnchor, fp, out BlockPlacement moved, view))
            {
                model.SetGridHandle(moved.Handle);
                _handles[id] = moved.Handle;
            }
            else if (_grid.TryPlaceBlock(placement.Anchor, fp, out BlockPlacement restored, view))
            {
                model.SetGridHandle(restored.Handle);
                _handles[id] = restored.Handle;
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
