using Game.Combat;
using Game.Events;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 30;

        private int _hp;
        private CombatEventHub _hub;
        private bool _isDead;

        public int Hp => _hp;
        public bool IsDead => _isDead;

        [Inject]
        public void Construct(CombatEventHub hub)
        {
            _hub = hub;
        }

        private void Awake()
        {
            _hp = maxHp;
        }

        public void ApplyDamage(int amount, HitContext context)
        {
            if (_isDead || amount <= 0) return;
            _hp = Mathf.Max(0, _hp - amount);
            if (_hp != 0) return;
            _isDead = true;
            _hub?.RaiseKill(new EnemyKillInfo(transform.position, context));
            gameObject.SetActive(false);
        }
    }
}
