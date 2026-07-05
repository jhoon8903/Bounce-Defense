namespace Game.Runtime.Grid
{
    // 그리드 셀 주소(열/행). row 0 = 최상단. 값 동등성을 가진 순수 값 타입.
    public readonly struct CellCoord
    {
        public readonly int Col;
        public readonly int Row;

        public CellCoord(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public override bool Equals(object obj) => obj is CellCoord other && Col == other.Col && Row == other.Row;
        public override int GetHashCode() => (Col * 397) ^ Row;
        public override string ToString() => $"({Col},{Row})";
    }
}
