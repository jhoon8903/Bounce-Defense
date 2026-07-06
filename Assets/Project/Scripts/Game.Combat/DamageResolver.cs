using Game.Core.Random;
using UnityEngine;

namespace Game.Combat
{
    public sealed class DamageResolver
    {
        private const float CritMultiplier = 1.5f;

        private readonly ModifierRegistry _modifiers;
        private readonly IRandom _random;

        public DamageResolver(ModifierRegistry modifiers, IRandom random)
        {
            _modifiers = modifiers;
            _random = random;
        }

        public void Resolve(HitContext ctx)
        {
            if (ctx?.Target == null) return;
            float working = ctx.BaseDamage;
            if (ctx.CanReceiveGlobalModifiers)
            {
                float additive = _modifiers?.GetAdditivePercentSum(ctx) ?? 0f;
                additive += ctx.BonusAdditivePercent;
                working *= 1f + additive;
            }

            if (ctx.CanCrit && _modifiers != null && _random != null)
            {
                ctx.IsCrit = _random.NextBool(_modifiers.GetCritChance(ctx));
                if (ctx.IsCrit) working *= CritMultiplier;
            }

            ctx.FinalDamage = Mathf.Max(0, Mathf.RoundToInt(working));
            ctx.Target.ApplyDamage(ctx.FinalDamage);
        }
        
        public void Resolve(IDamageable target, float baseDamage)
        {
            target?.ApplyDamage(Mathf.Max(0, Mathf.RoundToInt(baseDamage)));
        }
    }
}
