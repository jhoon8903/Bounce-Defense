using Game.Combat;
using Game.Core.Random;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public interface IBallEffectContext
    {
        IRandom Random { get; }

        void DamageEnemyRow(IDamageable originEnemy, float flatDamage, BallSourceType source);

        void SpawnClusterBall(Vector2 origin, float damage);
    }
}
