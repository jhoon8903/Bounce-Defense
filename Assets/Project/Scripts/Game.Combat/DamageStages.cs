using Game.Core.Random;
using UnityEngine;

namespace Game.Combat
{
    internal sealed class BaseDamageStage : IDamageStage
    {
        public void Process(HitContext context) => context.WorkingValue = context.BaseDamage;
    }

    internal sealed class AdditiveDamageStage : IDamageStage
    {
        private readonly ModifierRegistry _registry;
        public AdditiveDamageStage(ModifierRegistry registry) => _registry = registry;

        public void Process(HitContext context)
        {
            if (!context.CanReceiveGlobalModifiers) return;
            context.AdditivePercentSum = _registry.GetAdditivePercentSum(context);
            context.WorkingValue *= 1f + context.AdditivePercentSum;
        }
    }

    internal sealed class CritChanceStage : IDamageStage
    {
        private readonly ModifierRegistry _registry;
        private readonly IRandom _random;

        public CritChanceStage(ModifierRegistry registry, IRandom random)
        {
            _registry = registry;
            _random = random;
        }

        public void Process(HitContext context)
        {
            if (!context.CanCrit)
            {
                context.IsCrit = false;
                return;
            }

            float critChance = _registry.GetCritChance(context);
            context.IsCrit = _random.NextBool(critChance);
        }
    }

    internal sealed class CritDamageStage : IDamageStage
    {
        private const float CritMultiplier = 1.5f;
        public void Process(HitContext context)
        {
            if (context.IsCrit) context.WorkingValue *= CritMultiplier;
        }
    }

    internal sealed class RoundDamageStage : IDamageStage
    {
        public void Process(HitContext context) => context.FinalDamage = Mathf.Max(0, Mathf.RoundToInt(context.WorkingValue));
    }

    internal sealed class ApplyDamageStage : IDamageStage
    {
        public void Process(HitContext context) => context.Target?.ApplyDamage(context.FinalDamage, context);
    }
}
