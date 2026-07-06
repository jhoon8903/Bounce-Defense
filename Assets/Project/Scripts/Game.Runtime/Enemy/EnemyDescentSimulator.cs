using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 하강 시뮬레이션의 단일 소유자: 연속 하강·쌓임(아래 이웃 추종)·그리드 재등록·방어선 침범 판정.
    // 명단(models/views/handles)은 EnemyController가 소유하고 여기는 읽기+핸들 갱신만 한다.
    // 침범은 onBreach 콜백으로 컨트롤러에 보고(이벤트 발화·디스폰 권한은 컨트롤러).
    public sealed class EnemyDescentSimulator
    {
        private readonly GridController _grid;
        private readonly Dictionary<string, EnemyModel> _models;
        private readonly Dictionary<string, EnemyView> _views;
        private readonly Dictionary<string, int> _handles;
        private readonly Action<string, int> _onBreach; // (id, breachDamage)
        private readonly List<string> _order = new();
        private readonly Comparison<string> _cmpByRowDesc; // 캐시(인스턴스 메서드그룹→델리게이트 매 틱 할당 방지)

        public EnemyDescentSimulator(
            GridController grid,
            Dictionary<string, EnemyModel> models,
            Dictionary<string, EnemyView> views,
            Dictionary<string, int> handles,
            Action<string, int> onBreach)
        {
            _grid = grid;
            _models = models;
            _views = views;
            _handles = handles;
            _onBreach = onBreach;
            _cmpByRowDesc = CompareByRowDescending;
        }

        // 하강 1틱(입장 완료된 적만). 하단(행 인덱스 큰=화면 아래) 우선 → 아래가 먼저 비우면 위가 같은 틱에 이어 내려감.
        public void Tick(float deltaTime)
        {
            if (_models.Count == 0) return;
            _order.Clear();
            foreach (KeyValuePair<string, EnemyModel> kv in _models)
                if (!kv.Value.IsEntering && !kv.Value.IsBreaching) _order.Add(kv.Key);
            if (_order.Count == 0) return;
            _order.Sort(_cmpByRowDesc);
            float cellSize = _grid.CellSize;

            for (int i = 0; i < _order.Count; i++)
            {
                string id = _order[i];
                if (!_models.TryGetValue(id, out EnemyModel model) || model.IsEntering || model.IsBreaching) continue;
                DescendOne(id, model, deltaTime, cellSize);
            }
        }

        private int CompareByRowDescending(string a, string b) => RowOf(b).CompareTo(RowOf(a));

        private int RowOf(string id)
        {
            if (_handles.TryGetValue(id, out int h) && _grid.TryGetPlacement(h, out BlockPlacement p)) return p.Anchor.Row;
            return -1;
        }

        // 한 적의 1틱 하강. 이산 '칸 비었나' 게이트가 아니라 연속 '아래 이웃과 안전거리' 로 판정 → 스터터 없음.
        //  - 아래 이웃 있음 → 그 적의 실제 위치 기준 한 칸 위까지만 내려가 얹힘(같은 속도면 대열째 쭉, 빠른 적은 위에 얹혀 감속).
        //  - 아래 비었음   → 방어선(바닥)까지 자유 하강. 바닥 도달 시 침범 보고(컨트롤러가 이벤트+디스폰).
        //  - 그리드 재등록(배치 정합)은 셀 경계 넘을 때 유지하되, 움직임 자체는 위치 기반.
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
                // 방어선 도달 → 컨트롤러에 침범 보고(베이스 HP 감소 + 디스폰은 컨트롤러/StageController 책임).
                model.SetPosition(new Vector2(x, defenseFloorY));
                int breach = model.Definition != null ? model.Definition.BreachDamage : 0;
                _onBreach?.Invoke(id, breach);
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

        // 중심이 다음 행 중심 이하로 내려갔고 그 행이 비었으면 그리드 셀을 한 행 아래로 재등록(배치 정합).
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
                _handles[id] = moved.Handle;
            }
            else if (_grid.TryPlaceBlock(placement.Anchor, fp, out BlockPlacement restored, view))
            {
                _handles[id] = restored.Handle;
            }
        }
    }
}
