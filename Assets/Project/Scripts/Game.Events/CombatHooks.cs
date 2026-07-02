using Game.Combat;

namespace Game.Events
{
    public interface IOnLaunchHook
    {
        void OnLaunch(BallLaunchInfo info);
    }

    public interface IOnWallBounceHook
    {
        void OnWallBounce(BallWallBounceInfo info);
    }

    public interface IOnHitHook
    {
        void OnHit(HitContext context);
    }

    public interface IOnKillHook
    {
        void OnKill(EnemyKillInfo info);
    }
}
