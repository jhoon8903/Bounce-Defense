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

        public void Initialize(int cols, int rows, float cellSize, Vector2 origin)
        {
            _geometry = new GridGeometry(cols, rows, cellSize, origin);
            _map = new GridMap(_geometry.Cols, _geometry.Rows);
            IsInitialized = true;
            Raise();
        }

        // ---- 기하 (순수) ----
        public Vector2 CellToWorld(int col, int row) => _geometry.CellToWorld(col, row);
        public Vector2 FootprintWorldCenter(CellCoord anchor, Footprint fp) => _geometry.FootprintWorldCenter(anchor, fp);
        public Vector2 FootprintWorldSize(Footprint fp) => _geometry.FootprintWorldSize(fp);

        // ---- 점유 조회 (순수) ----
        public bool CanPlace(CellCoord anchor, Footprint fp) => _map != null && _map.CanPlace(anchor, fp);
        // 하강 재등록용: ignoreHandle(자기 자신) 겹침 허용 점유 조회.
        public bool CanPlace(CellCoord anchor, Footprint fp, int ignoreHandle) => _map != null && _map.CanPlace(anchor, fp, ignoreHandle);
        public int OccupantHandleAt(int col, int row) => _map != null ? _map.OccupantAt(col, row) : GridMap.Empty;

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
