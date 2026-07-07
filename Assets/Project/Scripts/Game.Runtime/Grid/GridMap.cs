using System;
using System.Collections.Generic;

namespace Game.Runtime.Grid
{
    public sealed class GridMap
    {
        public const int Empty = 0;

        private readonly int _cols;
        private readonly int _rows;
        private readonly int[] _cells;
        private readonly Dictionary<int, List<int>> _cellsByHandle;
        private readonly Stack<List<int>> _listPool = new();

        public int Cols => _cols;
        public int Rows => _rows;

        public GridMap(int cols, int rows)
        {
            _cols = cols < 1 ? 1 : cols;
            _rows = rows < 1 ? 1 : rows;
            _cells = new int[_cols * _rows];
            _cellsByHandle = new Dictionary<int, List<int>>();
        }

        private int Index(int col, int row) => row * _cols + col;

        public bool FitsFootprint(CellCoord anchor, Footprint fp) =>
            anchor is { Col: >= 0, Row: >= 0 } &&
            anchor.Col + fp.Width <= _cols &&
            anchor.Row + fp.Height <= _rows;

        public bool CanPlace(CellCoord anchor, Footprint fp)
        {
            if (!FitsFootprint(anchor, fp)) return false;
            for (int dy = 0; dy < fp.Height; dy++)
            {
                for (int dx = 0; dx < fp.Width; dx++)
                {
                    if (_cells[Index(anchor.Col + dx, anchor.Row + dy)] != Empty) return false;
                }
            }
            return true;
        }

        public bool CanPlace(CellCoord anchor, Footprint fp, int ignoreHandle)
        {
            if (!FitsFootprint(anchor, fp)) return false;
            for (int dy = 0; dy < fp.Height; dy++)
            {
                for (int dx = 0; dx < fp.Width; dx++)
                {
                    int h = _cells[Index(anchor.Col + dx, anchor.Row + dy)];
                    if (h != Empty && h != ignoreHandle) return false;
                }
            }
            return true;
        }

        public bool Place(int handle, CellCoord anchor, Footprint fp)
        {
            if (handle == Empty) return false;
            if (!CanPlace(anchor, fp)) return false;
            if (!_cellsByHandle.TryGetValue(handle, out List<int> list))
            {
                list = _listPool.Count > 0 ? _listPool.Pop() : new List<int>(4);
                list.Clear();
                _cellsByHandle[handle] = list;
            }
            for (int dy = 0; dy < fp.Height; dy++)
            {
                for (int dx = 0; dx < fp.Width; dx++)
                {
                    int idx = Index(anchor.Col + dx, anchor.Row + dy);
                    _cells[idx] = handle;
                    list.Add(idx);
                }
            }
            return true;
        }

        public bool Remove(int handle)
        {
            if (!_cellsByHandle.TryGetValue(handle, out List<int> list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (_cells[list[i]] == handle) _cells[list[i]] = Empty;
            }
            list.Clear();
            _listPool.Push(list);
            _cellsByHandle.Remove(handle);
            return true;
        }

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
            _cellsByHandle.Clear();
        }

        public int OccupantAt(int col, int row)
        {
            if (col < 0 || col >= _cols || row < 0 || row >= _rows) return Empty;
            return _cells[Index(col, row)];
        }
    }
}
