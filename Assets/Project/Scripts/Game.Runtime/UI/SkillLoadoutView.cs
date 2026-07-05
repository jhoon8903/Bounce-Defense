using System.Collections.Generic;
using Game.Roguelike;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    // 보유 스킬 로드아웃(SkillMonitor, 스펙 §47 Active4/Passive2). PlayerLoadout 구독.
    // 슬롯은 SkillSlotView 배열(직접 ref) — 런타임 Find 없음. 획득 순서대로 채우고 남는 칸은 비움.
    public sealed class SkillLoadoutView : UiView<PlayerLoadout>
    {
        [SerializeField] private SkillSlotView[] activeSlots;   // Slot0..3
        [SerializeField] private SkillSlotView[] passiveSlots;  // Slot1..2

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
