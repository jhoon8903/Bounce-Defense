using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
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
