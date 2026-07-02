using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    public sealed class ModifierRegistry
    {
        private readonly List<IDamageModifier> _modifiers = new();

        public void Register(IDamageModifier modifier)
        {
            if (modifier == null || _modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }

        public void Unregister(IDamageModifier modifier) => _modifiers.Remove(modifier);

        public float GetAdditivePercentSum(HitContext context)
        {
            float sum = 0f;
            for (int i = 0; i < _modifiers.Count; i++)
            {
                IDamageModifier modifier = _modifiers[i];
                if (modifier.AppliesTo(context)) sum += modifier.AdditivePercent;
            }
            return sum;
        }

        public float GetCritChance(HitContext context)
        {
            float sum = 0f;
            for (var i = 0; i < _modifiers.Count; i++)
            {
                IDamageModifier modifier = _modifiers[i];
                if (modifier.AppliesTo(context)) sum += modifier.CritChanceBonus;
            }
            return Mathf.Clamp01(sum);
        }
    }
}
