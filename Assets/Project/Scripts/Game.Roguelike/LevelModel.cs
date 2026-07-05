using System;
using Game.Core.Observer;
using UnityEngine;

namespace Game.Roguelike
{
    // 킬 기반 XP 레벨업(결정 B, 스펙 §3 "처치 수 충족"). 킬 → XP 누적 → 임계 도달 → 레벨업 → OnLevelUp(드래프트 트리거).
    // Observable → LevelProgressView(XP 바)가 구독. 커브는 데이터 주도(생성자 주입, StageDefinition에서).
    public sealed class LevelModel : Observable
    {
        private readonly int _xpPerKill;
        private readonly int _baseXpToNext;
        private readonly int _xpGrowthPerLevel;

        private int _level = 1;
        private int _xp;          // 현재 레벨에서 쌓인 XP
        private int _xpToNext;    // 다음 레벨까지 필요한 XP
        private int _pendingLevelUps;

        public int Level => _level;
        public int Xp => _xp;
        public int XpToNext => _xpToNext;
        public float Progress => _xpToNext > 0 ? (float)_xp / _xpToNext : 0f;
        public int PendingLevelUps => _pendingLevelUps;

        // 레벨업 발생 시 발화(컨트롤러가 드래프트 오픈). 뷰는 Observable로 바만 갱신.
        public event Action OnLevelUp;

        public LevelModel(int xpPerKill, int baseXpToNext, int xpGrowthPerLevel)
        {
            _xpPerKill = Mathf.Max(1, xpPerKill);
            _baseXpToNext = Mathf.Max(1, baseXpToNext);
            _xpGrowthPerLevel = Mathf.Max(0, xpGrowthPerLevel);
            _xpToNext = ThresholdFor(_level);
            Raise();
        }

        // 레벨 L → L+1 필요 XP. L1 기준 base, 레벨마다 growth 가산(선형 곡선).
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

        // 컨트롤러가 드래프트 한 건 처리할 때마다 소비(멀티 레벨업 큐잉).
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
