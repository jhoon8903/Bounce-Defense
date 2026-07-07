using Game.Combat;

namespace Game.Runtime.Combat
{
    public interface IBallModule
    {
        void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services);
    }
}
