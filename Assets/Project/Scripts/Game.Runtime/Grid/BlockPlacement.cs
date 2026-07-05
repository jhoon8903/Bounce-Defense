using UnityEngine;

namespace Game.Runtime.Grid
{
    // 배치 성공 결과. 블록의 위치·크기와 셀/핸들 정보를 담는다(성공/실패 판정은 TryPlaceBlock의 bool 반환).
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
    }
}
