using Game.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // Ice 볼 상시 추가뎀(§199·§178 가산 버킷): Ice 볼 직격에만 +10/15/20% 가산. 크리 보너스 없음.
    // Ice는 액티브지만 이 가산은 패시브형 모디파이어 — SkillRuntime이 액티브 스캔에서도 레지스트리에 등록한다.
    // self-gating(SourceBallType==Ice)이라 Normal/Fire 등 다른 볼엔 안 걸림.
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
