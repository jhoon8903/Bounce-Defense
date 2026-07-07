namespace Game.Runtime.Grid
{
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
