using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Grid
{
    public sealed class GridModel : Observable
    {
        private GridGeometry _geometry;
        private GridMap _map;

        public bool IsInitialized { get; private set; }
        public int Cols => _map?.Cols ?? 0;
        public int Rows => _map?.Rows ?? 0;
        public float CellSize => _geometry.CellSize;

        public void Initialize(int cols, int rows, float cellSize, Vector2 origin)
        {
            _geometry = new GridGeometry(cols, rows, cellSize, origin);
            _map = new GridMap(_geometry.Cols, _geometry.Rows);
            IsInitialized = true;
            Raise();
        }

        public Vector2 CellToWorld(int col, int row) => _geometry.CellToWorld(col, row);
        public CellCoord WorldToCell(Vector2 world) => _geometry.WorldToCell(world);
        public Vector2 FootprintWorldCenter(CellCoord anchor, Footprint fp) => _geometry.FootprintWorldCenter(anchor, fp);
        public Vector2 FootprintWorldSize(Footprint fp) => _geometry.FootprintWorldSize(fp);

        public bool CanPlace(CellCoord anchor, Footprint fp) => _map != null && _map.CanPlace(anchor, fp);
        public bool CanPlace(CellCoord anchor, Footprint fp, int ignoreHandle) => _map != null && _map.CanPlace(anchor, fp, ignoreHandle);
        public int OccupantHandleAt(int col, int row) => _map?.OccupantAt(col, row) ?? GridMap.Empty;

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
