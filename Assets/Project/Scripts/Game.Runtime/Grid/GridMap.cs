using System;
using System.Collections.Generic;

namespace Game.Runtime.Grid
{
    // 순수 점유 엔진 — UnityEngine 의존 없음(에디트모드 단위 테스트 가능).
    // 셀당 int 핸들(0=빈칸). 멀티셀 블록은 같은 핸들을 겹치는 모든 셀에 기록 → 모든 겹치는 행/열 자동 포함.
    // Unity 참조(IDamageable/Transform/Collider2D)는 절대 여기 들어오지 않는다(= GridController가 int->객체 브리지 소유).
    public sealed class GridMap
    {
        public const int Empty = 0;

        private readonly int _cols;
        private readonly int _rows;
        private readonly int[] _cells;                              // row*Cols + col -> 핸들
        private readonly Dictionary<int, List<int>> _cellsByHandle; // 핸들 -> 점유 셀 인덱스(제거 O(풋프린트))
        private readonly Stack<List<int>> _listPool = new();        // 셀 버킷 재사용(핸들이 단조증가라 하강 재등록마다 new List 할당하던 것 제거)

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

        // 풋프린트가 보드 경계 안에 들어오는지(클램프 아님, 판정만).
        public bool FitsFootprint(CellCoord anchor, Footprint fp) =>
            anchor.Col >= 0 && anchor.Row >= 0 &&
            anchor.Col + fp.Width <= _cols &&
            anchor.Row + fp.Height <= _rows;

        // 경계 안 + 모든 셀이 빈칸.
        public bool CanPlace(CellCoord anchor, Footprint fp)
        {
            if (!FitsFootprint(anchor, fp)) return false;
            for (int dy = 0; dy < fp.Height; dy++)
                for (int dx = 0; dx < fp.Width; dx++)
                    if (_cells[Index(anchor.Col + dx, anchor.Row + dy)] != Empty) return false;
            return true;
        }

        // 경계 안 + 모든 셀이 빈칸이거나 ignoreHandle 소유. 하강 재등록에서 자기 자신과 겹치는 셀은 무시(핸들 churn 없이 점유 조회).
        public bool CanPlace(CellCoord anchor, Footprint fp, int ignoreHandle)
        {
            if (!FitsFootprint(anchor, fp)) return false;
            for (int dy = 0; dy < fp.Height; dy++)
                for (int dx = 0; dx < fp.Width; dx++)
                {
                    int h = _cells[Index(anchor.Col + dx, anchor.Row + dy)];
                    if (h != Empty && h != ignoreHandle) return false;
                }
            return true;
        }

        // 배치 성공 시 true. 경계 밖/겹침이면 아무것도 안 쓰고 false(리젝트, 클램프 없음).
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
                for (int dx = 0; dx < fp.Width; dx++)
                {
                    int idx = Index(anchor.Col + dx, anchor.Row + dy);
                    _cells[idx] = handle;
                    list.Add(idx);
                }
            return true;
        }

        public bool Remove(int handle)
        {
            if (!_cellsByHandle.TryGetValue(handle, out List<int> list)) return false;
            for (int i = 0; i < list.Count; i++)
                if (_cells[list[i]] == handle) _cells[list[i]] = Empty;
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
