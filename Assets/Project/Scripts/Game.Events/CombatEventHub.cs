using System;
using Game.Combat;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Events
{
    public sealed class CombatEventHub
    {
        public event Action OnKill;
        public event Action<int> OnBreach;
        public event Action<EnemyView, Vector2, int, bool, Vector2, BallSourceType, DamageKind> OnHit;
        public event Action<Vector2, float> OnExplosion;
        public event Action<Vector2> OnLaserRow;
        public event Action<Vector2> OnClusterBurst;
        public event Action<Vector2> OnEnemyDeath;
        public event Action<Vector2> OnBaseHit;

        public void RaiseKill() => OnKill?.Invoke();
        public void RaiseBreach(int breachDamage) => OnBreach?.Invoke(breachDamage);
        public void RaiseHit(EnemyView view, Vector2 pos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType, DamageKind kind) => OnHit?.Invoke(view, pos, amount, isCrit, hitDir, sourceType, kind);
        public void RaiseExplosion(Vector2 center, float radius) => OnExplosion?.Invoke(center, radius);
        public void RaiseLaserRow(Vector2 center) => OnLaserRow?.Invoke(center);
        public void RaiseClusterBurst(Vector2 center) => OnClusterBurst?.Invoke(center);
        public void RaiseEnemyDeath(Vector2 pos) => OnEnemyDeath?.Invoke(pos);
        public void RaiseBaseHit(Vector2 pos) => OnBaseHit?.Invoke(pos);
    }
}
