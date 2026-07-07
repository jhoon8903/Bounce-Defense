using Game.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public sealed class IceBonusModifier : IDamageModifier
    {
        public IceBonusModifier(SkillDefinition skill, int level)
        {
            AdditivePercent = skill != null ? skill.GetIceBonusPercent(level) : 0f;
        }

        public bool AppliesTo(HitContext context) => context.SourceBallType == BallSourceType.Ice;
        public float AdditivePercent { get; }

        public float CritChanceBonus => 0f;
    }
}
