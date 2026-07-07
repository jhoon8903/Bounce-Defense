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

        private int _nextHandle = 1;

        public GridController(GridConfig config)
        {
            _config = config;
        }

        public bool IsReady => Model is { IsInitialized: true };
        public int Cols => Model?.Cols ?? (_config != null ? _config.Cols : 9);
        public int Rows => Model?.Rows ?? (_config != null ? _config.Rows : 13);
        public float CellSize => Model?.CellSize ?? (_config != null ? _config.CellSize : 1f);
        public Vector2 Origin { get; private set; } = DefaultOrigin;

        public GridModel Model { get; private set; }

        public void SetOrigin(Vector2 origin) => Origin = origin;

        protected override void OnInitialize()
        {
            int cols = _config != null ? _config.Cols : 9;
            int rows = _config != null ? _config.Rows : 13;
            float cell = _config != null ? _config.CellSize : 1f;
            Model = new GridModel();
            Model.Initialize(cols, rows, cell, Origin);
        }

        protected override void OnReset() => Clear();
        protected override void OnDispose() => Clear();
        protected override void OnTick(float deltaTime) { }
        protected override void OnFixedTick(float fixedDeltaTime) { }

        public bool TryPlaceBlock(CellCoord anchor, Footprint footprint, out BlockPlacement placement, IDamageable occupant = null)
        {
            placement = default;
            if (!IsReady) return false;
            if (!Model.CanPlace(anchor, footprint)) return false;

            int handle = _nextHandle++;
            if (!Model.Place(handle, anchor, footprint)) return false;

            Vector2 center = Model.FootprintWorldCenter(anchor, footprint);
            Vector2 size = Model.FootprintWorldSize(footprint);
            placement = new BlockPlacement(handle, anchor, footprint, center, size);
            _placements[handle] = placement;
            if (occupant != null) _occupants[handle] = occupant;
            return true;
        }

        public void RemoveBlock(int handle)
        {
            Model?.Remove(handle);
            _placements.Remove(handle);
            _occupants.Remove(handle);
        }

        public void Clear()
        {
            Model?.Clear();
            _placements.Clear();
            _occupants.Clear();
            _nextHandle = 1;
        }

        public Vector2 CellToWorld(int col, int row) => Model?.CellToWorld(col, row) ?? default;

        public bool IsCellOccupied(int col, int row) => Model != null && Model.OccupantHandleAt(col, row) != GridMap.Empty;
        public int OccupantHandleAt(int col, int row) => Model?.OccupantHandleAt(col, row) ?? GridMap.Empty;
        public bool TryGetPlacement(int handle, out BlockPlacement placement) => _placements.TryGetValue(handle, out placement);
        public bool TryGetOccupant(int handle, out IDamageable occupant) => _occupants.TryGetValue(handle, out occupant);
    }
}
