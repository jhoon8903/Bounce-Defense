using UnityEngine;

namespace Game.Runtime.Grid
{
    // 배치 성공 결과. 스포너/레이저가 블록을 위치·크기 지정하고 셀/행 인덱스를 얻는 데 필요한 전부.
    // WorldSize는 BoxCollider2D.size(핀볼 반사) 및 스프라이트 스케일 타깃(footprint*cellSize).
    public readonly struct BlockPlacement
    {
        public readonly int Handle;
        public readonly CellCoord Anchor;   // 좌상단 셀
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

        public bool IsValid => Handle > 0;
    }
}
