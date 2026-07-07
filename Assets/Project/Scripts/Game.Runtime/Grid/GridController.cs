using System.Collections.Generic;
using Game.Combat;
using Game.Core.Mvc;
using UnityEngine;

namespace Game.Runtime.Grid
{
    public sealed class GridController : BaseController
    {
        private static readonly Vector2 DefaultOrigin = new(0f, 1.27f);

        private readonly GridConfig _config;

        private readonly Dictionary<int, BlockPlacement> _placements = new();
        private readonly Dictionary<int, IDamageable> _occupants = new();

        private GridModel _model;
        private Vector2 _origin = DefaultOrigin;
        private int _nextHandle = 1;

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

        public Vector2 CellToWorld(int col, int row) => _model != null ? _model.CellToWorld(col, row) : default;

        public bool IsCellOccupied(int col, int row) => _model != null && _model.OccupantHandleAt(col, row) != GridMap.Empty;
        public int OccupantHandleAt(int col, int row) => _model != null ? _model.OccupantHandleAt(col, row) : GridMap.Empty;
        public bool TryGetPlacement(int handle, out BlockPlacement placement) => _placements.TryGetValue(handle, out placement);
        public bool TryGetOccupant(int handle, out IDamageable occupant) => _occupants.TryGetValue(handle, out occupant);
    }
}
