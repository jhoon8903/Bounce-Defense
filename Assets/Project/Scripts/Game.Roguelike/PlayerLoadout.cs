using System.Collections.Generic;
using Game.Core.Observer;
using Game.Skills;

namespace Game.Roguelike
{
    public sealed class PlayerLoadout : Observable
    {
        public const int ActiveCap = 4;
        public const int PassiveCap = 2;

        private readonly Dictionary<SkillDefinition, int> _levels = new();
        private readonly List<SkillDefinition> _order = new();

        public int ActiveCount { get; private set; }

        public int PassiveCount { get; private set; }

        public bool Owns(SkillDefinition skill) => skill != null && _levels.ContainsKey(skill);

        public int LevelOf(SkillDefinition skill) => skill != null && _levels.TryGetValue(skill, out int lv) ? lv : 0;

        public bool IsFull(SkillCategory category) => category == SkillCategory.Active ? ActiveCount >= ActiveCap : PassiveCount >= PassiveCap;
        
        public void Acquire(SkillDefinition skill)
        {
            if (skill == null || _levels.ContainsKey(skill)) return;
            if (IsFull(skill.Category)) return;
            _levels[skill] = 1;
            _order.Add(skill);
            if (skill.Category == SkillCategory.Active) ActiveCount++; else PassiveCount++;
            Raise();
        }
        
        public void Upgrade(SkillDefinition skill)
        {
            if (skill == null || !_levels.TryGetValue(skill, out int lv)) return;
            if (lv >= skill.MaxLevel) return;
            _levels[skill] = lv + 1;
            Raise();
        }
        
        public void Apply(SkillCard card)
        {
            if (!card.IsValid) return;
            if (card.IsNew) Acquire(card.Definition);
            else Upgrade(card.Definition);
        }
        
        public void CopyOwned(SkillCategory category, List<KeyValuePair<SkillDefinition, int>> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _order.Count; i++)
            {
                SkillDefinition s = _order[i];
                if (s != null && s.Category == category && _levels.TryGetValue(s, out int lv))
                {
                    buffer.Add(new KeyValuePair<SkillDefinition, int>(s, lv));
                }
            }
        }

        // 재시작(§264): 스킬 0으로.
        public void ResetLoadout()
        {
            _levels.Clear();
            _order.Clear();
            ActiveCount = 0;
            PassiveCount = 0;
            Raise();
        }
    }
}
