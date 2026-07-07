using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    public sealed class EmeraldDaggerModifier : IDamageModifier
    {
        private static readonly float[] CritChance = { 0.20f, 0.30f, 0.40f };

        private readonly float _critChance;

        public EmeraldDaggerModifier(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            _critChance = CritChance[i];
        }

        public bool AppliesTo(HitContext context) => context.HitNormal.y > 0f;
        public float AdditivePercent => 0f;
        public float CritChanceBonus => _critChance;
    }
}
