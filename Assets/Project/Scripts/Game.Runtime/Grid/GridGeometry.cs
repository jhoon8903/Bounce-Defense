using UnityEngine;

namespace Game.Runtime.Grid
{
    public readonly struct GridGeometry
    {
        public readonly int Cols;
        public readonly int Rows;
        public readonly float CellSize;
        public readonly Vector2 Origin;
        private readonly float _halfColSpan;
        private readonly float _halfRowSpan;

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

        public Vector2 CellToWorld(int col, int row) => new(
            Origin.x + (col - _halfColSpan) * CellSize,
            Origin.y + (_halfRowSpan - row) * CellSize);

        public CellCoord WorldToCell(Vector2 world) => new(
            Mathf.RoundToInt((world.x - Origin.x) / CellSize + _halfColSpan),
            Mathf.RoundToInt(_halfRowSpan - (world.y - Origin.y) / CellSize));

        public Vector2 FootprintWorldCenter(CellCoord anchor, Footprint fp)
        {
            Vector2 topLeft = CellToWorld(anchor.Col, anchor.Row);
            Vector2 bottomRight = CellToWorld(anchor.Col + fp.Width - 1, anchor.Row + fp.Height - 1);
            return (topLeft + bottomRight) * 0.5f;
        }

        public Vector2 FootprintWorldSize(Footprint fp) => new(fp.Width * CellSize, fp.Height * CellSize);
    }
}
