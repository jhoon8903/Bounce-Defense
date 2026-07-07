using System.Collections.Generic;
using Game.Roguelike;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    public sealed class SkillLoadoutView : UiView<PlayerLoadout>
    {
        [SerializeField] private SkillSlotView[] activeSlots;
        [SerializeField] private SkillSlotView[] passiveSlots;

        private readonly List<KeyValuePair<SkillDefinition, int>> _buffer = new();

        protected override void RefreshView()
        {
            PlayerLoadout lo = Model;
            if (lo == null) return;
            Fill(activeSlots, SkillCategory.Active, lo);
            Fill(passiveSlots, SkillCategory.Passive, lo);
        }

        private void Fill(SkillSlotView[] slots, SkillCategory category, PlayerLoadout lo)
        {
            if (slots == null) return;
            lo.CopyOwned(category, _buffer);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                if (i < _buffer.Count) slots[i].SetSkill(_buffer[i].Key, _buffer[i].Value);
                else slots[i].SetEmpty();
            }
        }
    }
}
