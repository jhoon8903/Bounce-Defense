using Game.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public sealed class IceBonusModifier : IDamageModifier
    {
        private readonly float _percent;

        public IceBonusModifier(SkillDefinition skill, int level)
        {
            _percent = skill != null ? skill.GetIceBonusPercent(level) : 0f;
        }

        public bool AppliesTo(HitContext context) => context.SourceBallType == BallSourceType.Ice;
        public float AdditivePercent => _percent;
        public float CritChanceBonus => 0f;
    }
}
