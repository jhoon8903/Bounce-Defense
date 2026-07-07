using UnityEngine;

namespace Game.Runtime.Grid
{
    public readonly struct BlockPlacement
    {
        public readonly int Handle;
        public readonly CellCoord Anchor;
        public readonly Footprint Footprint;
        public readonly Vector2 WorldCenter;
        public readonly Vector2 WorldSize;

        public BlockPlacement(int handle, CellCoord anchor, Footprint footprint, Vector2 worldCenter, Vector2 worldSize)
        {
            Handle = handle;
            Anchor = anchor;
            Footprint = footprint;
            WorldCenter = worldCenter;
            WorldSize = worldSize;
        }
    }
}
