using System;
using Game.Core.Observer;
using UnityEngine;

namespace Game.Roguelike
{
    public sealed class LevelModel : Observable
    {
        private readonly int _xpPerKill;
        private readonly int _baseXpToNext;
        private readonly int _xpGrowthPerLevel;
        private readonly int _maxLevel;

        private int _level = 1;
        private int _xp;
        private int _xpToNext;
        private int _pendingLevelUps;

        public int Level => _level;
        public int Xp => _xp;
        public int XpToNext => _xpToNext;
        public float Progress => IsMaxLevel ? 1f : (_xpToNext > 0 ? (float)_xp / _xpToNext : 0f);
        public int PendingLevelUps => _pendingLevelUps;
        public bool IsMaxLevel => _level >= _maxLevel; // 최대 레벨(모든 선택지 소진) → EXP 바 풀·레벨업 중단
        
        public event Action OnLevelUp;

        public LevelModel(int xpPerKill, int baseXpToNext, int xpGrowthPerLevel, int maxLevel)
        {
            _xpPerKill = Mathf.Max(1, xpPerKill);
            _baseXpToNext = Mathf.Max(1, baseXpToNext);
            _xpGrowthPerLevel = Mathf.Max(0, xpGrowthPerLevel);
            _maxLevel = Mathf.Max(1, maxLevel);
            _xpToNext = ThresholdFor(_level);
            Raise();
        }
        
        private int ThresholdFor(int level) => _baseXpToNext + (level - 1) * _xpGrowthPerLevel;

        public void AddKill()
        {
            if (IsMaxLevel) return; // 최대 레벨 도달 → XP·레벨업 중단(EXP 바 풀 유지)
            _xp += _xpPerKill;
            bool leveled = false;
            while (_xp >= _xpToNext && _level < _maxLevel)
            {
                _xp -= _xpToNext;
                _level++;
                _xpToNext = ThresholdFor(_level);
                _pendingLevelUps++;
                leveled = true;
            }
            if (IsMaxLevel) _xp = 0; // 최대 레벨: 잔여 XP 버리고 바 풀
            Raise();
            if (leveled) OnLevelUp?.Invoke();
        }
        
        public bool TryConsumeLevelUp()
        {
            if (_pendingLevelUps <= 0) return false;
            _pendingLevelUps--;
            return true;
        }

        public void ResetProgression()
        {
            _level = 1;
            _xp = 0;
            _pendingLevelUps = 0;
            _xpToNext = ThresholdFor(_level);
            Raise();
        }
    }
}
