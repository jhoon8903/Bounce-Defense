namespace Game.Combat
{
    public interface IDamageModifier
    {
        bool AppliesTo(HitContext context);
        float AdditivePercent { get; }
        float CritChanceBonus { get; }
    }
}
