namespace Game.Runtime.Grid
{
    // 블록 풋프린트(셀 단위 W x H). 규약: Width=열 개수, Height=행 개수. 1x1/1x2/2x1/2x2.
    public readonly struct Footprint
    {
        public readonly int Width;
        public readonly int Height;

        public Footprint(int width, int height)
        {
            Width = width < 1 ? 1 : width;
            Height = height < 1 ? 1 : height;
        }

        public int CellCount => Width * Height;

        public static readonly Footprint Size1x1 = new(1, 1);
        public static readonly Footprint Size1x2 = new(1, 2); // 1열 x 2행
        public static readonly Footprint Size2x1 = new(2, 1); // 2열 x 1행
        public static readonly Footprint Size2x2 = new(2, 2);

        public override bool Equals(object obj) => obj is Footprint other && Width == other.Width && Height == other.Height;
        public override int GetHashCode() => (Width * 397) ^ Height;
        public override string ToString() => $"{Width}x{Height}";
    }
}
