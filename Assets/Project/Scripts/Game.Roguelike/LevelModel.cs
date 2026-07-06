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

        private int _level = 1;
        private int _xp;
        private int _xpToNext;
        private int _pendingLevelUps;

        public int Level => _level;
        public int Xp => _xp;
        public int XpToNext => _xpToNext;
        public float Progress => _xpToNext > 0 ? (float)_xp / _xpToNext : 0f;
        public int PendingLevelUps => _pendingLevelUps;
        
        public event Action OnLevelUp;

        public LevelModel(int xpPerKill, int baseXpToNext, int xpGrowthPerLevel)
        {
            _xpPerKill = Mathf.Max(1, xpPerKill);
            _baseXpToNext = Mathf.Max(1, baseXpToNext);
            _xpGrowthPerLevel = Mathf.Max(0, xpGrowthPerLevel);
            _xpToNext = ThresholdFor(_level);
            Raise();
        }
        
        private int ThresholdFor(int level) => _baseXpToNext + (level - 1) * _xpGrowthPerLevel;

        public void AddKill()
        {
            _xp += _xpPerKill;
            bool leveled = false;
            while (_xp >= _xpToNext)
            {
                _xp -= _xpToNext;
                _level++;
                _xpToNext = ThresholdFor(_level);
                _pendingLevelUps++;
                leveled = true;
            }
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
