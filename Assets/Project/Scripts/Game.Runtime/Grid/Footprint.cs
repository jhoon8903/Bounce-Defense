namespace Game.Runtime.Grid
{
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
        public static readonly Footprint Size1x2 = new(1, 2);
        public static readonly Footprint Size2x1 = new(2, 1);
        public static readonly Footprint Size2x2 = new(2, 2);

        public override bool Equals(object obj) => obj is Footprint other && Width == other.Width && Height == other.Height;
        public override int GetHashCode() => (Width * 397) ^ Height;
        public override string ToString() => $"{Width}x{Height}";
    }
}
