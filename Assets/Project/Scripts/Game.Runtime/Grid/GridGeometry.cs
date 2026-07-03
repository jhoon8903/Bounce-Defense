using UnityEngine;

namespace Game.Runtime.Grid
{
    // 셀 <-> 월드 좌표 순수 변환. 앵커 = 그리드 '중심'(씬 Grid 오브젝트, 0,1.27). row 0 = 최상단.
    // 9x13 / cell 1.0 / origin (0,1.27) 기준:
    //   CellToWorld(0,0)=(-4.0, 7.27), (8,12)=(4.0,-4.73), (4,6)=(0,1.27)=origin.
    public readonly struct GridGeometry
    {
        public readonly int Cols;
        public readonly int Rows;
        public readonly float CellSize;
        public readonly Vector2 Origin;
        private readonly float _halfColSpan; // (Cols-1)/2
        private readonly float _halfRowSpan; // (Rows-1)/2

        public GridGeometry(int cols, int rows, float cellSize, Vector2 origin)
        {
            Cols = cols < 1 ? 1 : cols;
            Rows = rows < 1 ? 1 : rows;
            CellSize = cellSize > 0f ? cellSize : 1f;
            Origin = origin;
            _halfColSpan = (Cols - 1) * 0.5f;
            _halfRowSpan = (Rows - 1) * 0.5f;
        }

        public bool InBounds(int col, int row) => col >= 0 && col < Cols && row >= 0 && row < Rows;

        // 셀 중심 월드 좌표.
        public Vector2 CellToWorld(int col, int row) => new(
            Origin.x + (col - _halfColSpan) * CellSize,
            Origin.y + (_halfRowSpan - row) * CellSize);

        // 월드 -> 가장 가까운 셀(round). 셀 ±0.5 밴드 안이면 그 셀로 귀속. 경계 밖도 셀 좌표를 그대로 반환(범위 검증은 InBounds).
        public CellCoord WorldToCell(Vector2 world) => new(
            Mathf.RoundToInt((world.x - Origin.x) / CellSize + _halfColSpan),
            Mathf.RoundToInt(_halfRowSpan - (world.y - Origin.y) / CellSize));

        // 풋프린트 월드 중심 = 좌상단 셀 중심과 우하단 셀 중심의 중점(선형 매핑이므로 정확).
        public Vector2 FootprintWorldCenter(CellCoord anchor, Footprint fp)
        {
            Vector2 topLeft = CellToWorld(anchor.Col, anchor.Row);
            Vector2 bottomRight = CellToWorld(anchor.Col + fp.Width - 1, anchor.Row + fp.Height - 1);
            return (topLeft + bottomRight) * 0.5f;
        }

        // 풋프린트 월드 크기 = (W*cell, H*cell). BoxCollider2D.size / 스프라이트 타깃 크기.
        public Vector2 FootprintWorldSize(Footprint fp) => new(fp.Width * CellSize, fp.Height * CellSize);
    }
}
