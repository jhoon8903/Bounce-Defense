namespace Game.Combat
{
    public interface IDamageable
    {
        void ApplyDamage(int amount, HitContext context);
    }
}
