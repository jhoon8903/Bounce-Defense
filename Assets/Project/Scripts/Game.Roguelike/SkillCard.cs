using Game.Skills;

namespace Game.Roguelike
{
    public readonly struct SkillCard
    {
        public readonly SkillDefinition Definition;
        public readonly int Level;
        public readonly bool IsNew;

        public SkillCard(SkillDefinition definition, int level, bool isNew)
        {
            Definition = definition;
            Level = level;
            IsNew = isNew;
        }

        public bool IsValid => Definition != null;
    }
}
