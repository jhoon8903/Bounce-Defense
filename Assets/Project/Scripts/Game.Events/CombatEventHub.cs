using System;
using Game.Combat;

namespace Game.Events
{
    public sealed class CombatEventHub
    {
        public event Action<BallLaunchInfo> OnLaunch;
        public event Action<BallWallBounceInfo> OnWallBounce;
        public event Action<HitContext> OnHit;
        public event Action<EnemyKillInfo> OnKill;
        public event Action<EnemyBreachInfo> OnBreach;

        public void RaiseLaunch(BallLaunchInfo info) => OnLaunch?.Invoke(info);
        public void RaiseWallBounce(BallWallBounceInfo info) => OnWallBounce?.Invoke(info);
        public void RaiseHit(HitContext context) => OnHit?.Invoke(context);
        public void RaiseKill(EnemyKillInfo info) => OnKill?.Invoke(info);
        public void RaiseBreach(EnemyBreachInfo info) => OnBreach?.Invoke(info);
    }
}
