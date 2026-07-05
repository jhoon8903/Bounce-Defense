using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    // Warm Tin Heart 패시브: NormalBall 직격에만 가산% (플랜 §208·§178). 크리 보너스 없음.
    // 레벨별 +20/30/40%. self-gating(AppliesTo)이라 Fire/Ice 등 특수볼엔 안 걸림 — "노멀만" 명세 그대로.
    public sealed class WarmTinModifier : IDamageModifier
    {
        private static readonly float[] Percent = { 0.20f, 0.30f, 0.40f };

        private readonly float _percent;

        public WarmTinModifier(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            _percent = Percent[i];
        }

        public bool AppliesTo(HitContext context) => context.SourceBallType == BallSourceType.Normal;
        public float AdditivePercent => _percent;
        public float CritChanceBonus => 0f;
    }
}
