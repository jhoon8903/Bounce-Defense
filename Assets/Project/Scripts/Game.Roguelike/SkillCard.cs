using Game.Skills;

namespace Game.Roguelike
{
    // 드로우 결과 1장. 표시 + 선택 적용에 필요한 것만.
    public readonly struct SkillCard
    {
        public readonly SkillDefinition Definition;
        public readonly int Level;   // 제안 레벨(신규=1, 업그레이드=보유+1)
        public readonly bool IsNew;  // true=신규 획득(NEW 뱃지), false=업그레이드

        public SkillCard(SkillDefinition definition, int level, bool isNew)
        {
            Definition = definition;
            Level = level;
            IsNew = isNew;
        }

        public bool IsValid => Definition != null;
    }
}
