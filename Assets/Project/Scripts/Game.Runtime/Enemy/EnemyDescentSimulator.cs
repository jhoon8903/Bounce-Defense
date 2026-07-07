using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyDescentSimulator
    {
        private readonly GridController _grid;
        private readonly Dictionary<string, EnemyModel> _models;
        private readonly Dictionary<string, EnemyView> _views;
        private readonly Dictionary<string, int> _handles;
        private readonly Action<string, int> _onBreach;
        private readonly List<string> _order = new();
        private readonly Comparison<string> _cmpByRowDesc;

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

            if (TryComputeBelowFloor(handle, placement, fp, cellSize, out float belowFloor))
            {
                float clampedY = Mathf.Max(desiredY, belowFloor);
                if (clampedY > model.Position.y) clampedY = model.Position.y;
                model.SetPosition(new Vector2(x, clampedY));
                MaybeReRegister(id, model, placement, fp, view, handle, cellSize);
                return;
            }

            float defenseFloorY = _grid.Model.FootprintWorldCenter(
                new CellCoord(placement.Anchor.Col, _grid.Rows - fp.Height), fp).y;
            if (desiredY < defenseFloorY)
            {
                model.SetPosition(new Vector2(x, defenseFloorY));
                int breach = model.Definition != null ? model.Definition.BreachDamage : 0;
                _onBreach?.Invoke(id, breach);
                return;
            }
            model.SetPosition(new Vector2(x, desiredY));
            MaybeReRegister(id, model, placement, fp, view, handle, cellSize);
        }

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
                        float belowY = bm.IsEntering ? bm.LandedY : bm.Position.y;
                        float allowed = belowY + (fp.Height + bm.Footprint.Height) * cellSize * 0.5f;
                        if (allowed > floorY) floorY = allowed;
                        found = true;
                    }
                    break;
                }
            }
            return found;
        }

        private void MaybeReRegister(string id, EnemyModel model, BlockPlacement placement, Footprint fp, EnemyView view, int handle, float cellSize)
        {
            float centerY = placement.WorldCenter.y;
            if (model.Position.y > centerY - cellSize) return;
            CellCoord nextAnchor = new CellCoord(placement.Anchor.Col, placement.Anchor.Row + 1);
            if (nextAnchor.Row + fp.Height > _grid.Rows) return;
            if (!_grid.Model.CanPlace(nextAnchor, fp, handle)) return;
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
