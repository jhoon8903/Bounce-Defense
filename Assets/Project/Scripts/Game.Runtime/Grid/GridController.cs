using System.Collections.Generic;
using Game.Combat;
using Game.Core.Mvc;
using UnityEngine;

namespace Game.Runtime.Grid
{
    // 배치 권한(placement authority). BallController를 그대로 미러링:
    //  - 생성자 = 순수 DI(엔진 작업 없음), OnInitialize에서 엔진 세팅.
    //  - 핸들 키 병렬 딕셔너리(placement / occupant).
    // 그리드 코어에는 적/HP/웨이브 로직 없음. 블록/적은 셀에 꽂힌다(occupant = IDamageable 브리지).
    // 하강/재등록은 EnemyDescentSimulator 소유 — 여기는 배치/조회만 (하강을 여기 중복 구현하지 말 것).
    public sealed class GridController : BaseController
    {
        private static readonly Vector2 DefaultOrigin = new(0f, 1.27f);

        private readonly GridConfig _config;

        private readonly Dictionary<int, BlockPlacement> _placements = new();
        private readonly Dictionary<int, IDamageable> _occupants = new();

        private GridModel _model;
        private Vector2 _origin = DefaultOrigin;
        private int _nextHandle = 1; // 0 = 빈칸 예약

        public GridController(GridConfig config)
        {
            _config = config;
        }

        public bool IsReady => _model != null && _model.IsInitialized;
        public int Cols => _model != null ? _model.Cols : (_config != null ? _config.Cols : 9);
        public int Rows => _model != null ? _model.Rows : (_config != null ? _config.Rows : 13);
        public float CellSize => _model != null ? _model.CellSize : (_config != null ? _config.CellSize : 1f);
        public Vector2 Origin => _origin;
        public GridModel Model => _model;

        // 앵커 원점(씬 'Grid' 빈 오브젝트 월드 좌표) 주입. Initialize 전에 호출 — BallController.SetCollectTarget 미러.
        public void SetOrigin(Vector2 origin) => _origin = origin;

        protected override void OnInitialize()
        {
            int cols = _config != null ? _config.Cols : 9;
            int rows = _config != null ? _config.Rows : 13;
            float cell = _config != null ? _config.CellSize : 1f;
            _model = new GridModel();
            _model.Initialize(cols, rows, cell, _origin);
        }

        protected override void OnReset() => Clear();
        protected override void OnDispose() => Clear();
        protected override void OnTick(float deltaTime) { }
        protected override void OnFixedTick(float fixedDeltaTime) { }

        // ---- 배치 API ----

        // 지정 앵커에 배치. 경계 밖/겹침이면 false(리젝트). occupant는 선택(그리드 코어는 null 허용).
        public bool TryPlaceBlock(CellCoord anchor, Footprint footprint, out BlockPlacement placement, IDamageable occupant = null)
        {
            placement = default;
            if (!IsReady) return false;
            if (!_model.CanPlace(anchor, footprint)) return false;

            int handle = _nextHandle++;
            if (!_model.Place(handle, anchor, footprint)) return false;

            Vector2 center = _model.FootprintWorldCenter(anchor, footprint);
            Vector2 size = _model.FootprintWorldSize(footprint);
            placement = new BlockPlacement(handle, anchor, footprint, center, size);
            _placements[handle] = placement;
            if (occupant != null) _occupants[handle] = occupant;
            return true;
        }

        public void RemoveBlock(int handle)
        {
            _model?.Remove(handle);
            _placements.Remove(handle);
            _occupants.Remove(handle);
        }

        public void Clear()
        {
            _model?.Clear();
            _placements.Clear();
            _occupants.Clear();
            _nextHandle = 1;
        }

        // ---- 기하 패스스루 (GridDebugView 시각화용) ----
        public Vector2 CellToWorld(int col, int row) => _model != null ? _model.CellToWorld(col, row) : default;

        // ---- 점유 조회 (하강 시뮬 + 디버그 시각화) ----
        public bool IsCellOccupied(int col, int row) => _model != null && _model.OccupantHandleAt(col, row) != GridMap.Empty;
        public int OccupantHandleAt(int col, int row) => _model != null ? _model.OccupantHandleAt(col, row) : GridMap.Empty;
        public bool TryGetPlacement(int handle, out BlockPlacement placement) => _placements.TryGetValue(handle, out placement);
        public bool TryGetOccupant(int handle, out IDamageable occupant) => _occupants.TryGetValue(handle, out occupant);
    }
}
