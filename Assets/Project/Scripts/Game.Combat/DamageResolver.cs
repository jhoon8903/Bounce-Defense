using Game.Core.Random;

namespace Game.Combat
{
    public sealed class DamageResolver
    {
        private readonly IDamageStage[] _stages;

        public DamageResolver(ModifierRegistry modifierRegistry, IRandom random)
        {
            _stages = new IDamageStage[]
            {
                new BaseDamageStage(),
                new AdditiveDamageStage(modifierRegistry),
                new CritChanceStage(modifierRegistry, random),
                new CritDamageStage(),
                new RoundDamageStage(),
                new ApplyDamageStage(),
            };
        }

        public void Resolve(HitContext context)
        {
            foreach (IDamageStage stage in _stages) stage.Process(context);
        }
    }
}
