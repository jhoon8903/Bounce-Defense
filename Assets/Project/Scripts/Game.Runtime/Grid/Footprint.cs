using System;
using UnityEngine;

namespace Game.Runtime.Grid
{
    // 블록 풋프린트(셀 단위 W x H). 규약: Width=열 개수, Height=행 개수. 1x1/1x2/2x1/2x2.
    public readonly struct Footprint : IEquatable<Footprint>
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

        // "Block_2x2" / "2x2" -> (2,2). 규약 WxH.
        public static bool TryFromBlockName(string blockName, out Footprint footprint)
        {
            footprint = Size1x1;
            if (string.IsNullOrEmpty(blockName)) return false;
            int underscore = blockName.LastIndexOf('_');
            string dims = underscore >= 0 ? blockName.Substring(underscore + 1) : blockName;
            int x = dims.IndexOf('x');
            if (x <= 0 || x >= dims.Length - 1) return false;
            if (int.TryParse(dims.Substring(0, x), out int w) &&
                int.TryParse(dims.Substring(x + 1), out int h))
            {
                footprint = new Footprint(w, h);
                return true;
            }
            return false;
        }

        public bool Equals(Footprint other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Footprint other && Equals(other);
        public override int GetHashCode() => (Width * 397) ^ Height;
        public static bool operator ==(Footprint a, Footprint b) => a.Equals(b);
        public static bool operator !=(Footprint a, Footprint b) => !a.Equals(b);
        public override string ToString() => $"{Width}x{Height}";
    }
}
