using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    public sealed class AmethystDaggerModifier : IDamageModifier
    {
        private static readonly float[] CritChance = { 0.10f, 0.20f, 0.30f };

        public AmethystDaggerModifier(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            CritChanceBonus = CritChance[i];
        }

        public bool AppliesTo(HitContext context) => context.HitNormal.y < 0f;
        public float AdditivePercent => 0f;
        public float CritChanceBonus { get; }
    }
}
