using System;
using System.Collections.Generic;
using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Grid
{
    // 보드 상태 Observable: 기하(GridGeometry) + 점유(GridMap). BallModel 규율을 따름 —
    // 뷰에 보이는 변경(place/remove/clear)에서만 Raise, 순수 조회는 Raise 안 함.
    public sealed class GridModel : Observable
    {
        private GridGeometry _geometry;
        private GridMap _map;

        public bool IsInitialized { get; private set; }
        public int Cols => _map != null ? _map.Cols : 0;
        public int Rows => _map != null ? _map.Rows : 0;
        public float CellSize => _geometry.CellSize;
        public Vector2 Origin => _geometry.Origin;

        public void Initialize(int cols, int rows, float cellSize, Vector2 origin)
        {
            _geometry = new GridGeometry(cols, rows, cellSize, origin);
            _map = new GridMap(_geometry.Cols, _geometry.Rows);
            IsInitialized = true;
            Raise();
        }

        // ---- 기하 (순수) ----
        public Vector2 CellToWorld(int col, int row) => _geometry.CellToWorld(col, row);
        public Vector2 CellToWorld(CellCoord cell) => _geometry.CellToWorld(cell.Col, cell.Row);
        public CellCoord WorldToCell(Vector2 world) => _geometry.WorldToCell(world);
        public bool InBounds(CellCoord cell) => _geometry.InBounds(cell.Col, cell.Row);
        public Vector2 FootprintWorldCenter(CellCoord anchor, Footprint fp) => _geometry.FootprintWorldCenter(anchor, fp);
        public Vector2 FootprintWorldSize(Footprint fp) => _geometry.FootprintWorldSize(fp);

        // ---- 점유 조회 (순수) ----
        public bool CanPlace(CellCoord anchor, Footprint fp) => _map != null && _map.CanPlace(anchor, fp);
        public bool TryFindFreeAnchor(Footprint fp, out CellCoord anchor)
        {
            if (_map != null) return _map.TryFindFreeAnchor(fp, out anchor);
            anchor = default;
            return false;
        }
        public int OccupantHandleAt(int col, int row) => _map != null ? _map.OccupantAt(col, row) : GridMap.Empty;
        public void RowsOf(int handle, List<int> buffer) => _map?.RowsOf(handle, buffer);
        public IReadOnlyList<int> CellsOf(int handle) => _map != null ? _map.CellsOf(handle) : Array.Empty<int>();
        public int OccupantsInRow(int row, List<int> buffer)
        {
            if (_map != null) return _map.OccupantsInRow(row, buffer);
            buffer.Clear();
            return 0;
        }

        // ---- 점유 변경 (성공 시 Raise) ----
        public bool Place(int handle, CellCoord anchor, Footprint fp)
        {
            if (_map == null || !_map.Place(handle, anchor, fp)) return false;
            Raise();
            return true;
        }
        public void Remove(int handle)
        {
            if (_map != null && _map.Remove(handle)) Raise();
        }
        public void Clear()
        {
            if (_map == null) return;
            _map.Clear();
            Raise();
        }
    }
}
