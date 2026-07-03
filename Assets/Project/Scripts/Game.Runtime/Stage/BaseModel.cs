using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Stage
{
    // 방어선 베이스 HP(개발플랜 §43: 300, 즉사 아님·감소형·0이면 실패). Observable → HUD 초록 바가 구독.
    public sealed class BaseModel : Observable
    {
        private int _hp;
        private int _maxHp;

        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDead => _hp <= 0;
        // 성공 화면 별점 = 잔여 베이스HP%(§49).
        public float RemainingPercent => _maxHp > 0 ? (float)_hp / _maxHp : 0f;

        public void Initialize(int maxHp)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _hp = _maxHp;
            Raise();
        }

        public void TakeDamage(int amount)
        {
            if (_hp <= 0 || amount <= 0) return;
            _hp = Mathf.Max(0, _hp - amount);
            Raise();
        }
    }
}
