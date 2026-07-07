using System.Collections.Generic;
using Game.Core.Random;
using Game.Skills;

namespace Game.Roguelike
{
    public sealed class CardDrawService
    {
        private readonly SkillDatabase _database;
        private readonly PlayerLoadout _loadout;
        private readonly IRandom _random;
        private readonly List<SkillCard> _candidates = new();

        public CardDrawService(SkillDatabase database, PlayerLoadout loadout, IRandom random)
        {
            _database = database;
            _loadout = loadout;
            _random = random;
        }
        
        public int CandidateCount()
        {
            BuildCandidates();
            return _candidates.Count;
        }

        public List<SkillCard> Draw(int count)
        {
            BuildCandidates();
            _random.Shuffle(_candidates);
            int n = count < _candidates.Count ? count : _candidates.Count;
            List<SkillCard> result = new List<SkillCard>(n);
            for (int i = 0; i < n; i++)
            {
                result.Add(_candidates[i]);
            }
            return result;
        }

        private void BuildCandidates()
        {
            _candidates.Clear();
            if (_database == null || _loadout == null) return;
            AddCategory(_database.ActiveSkills);
            AddCategory(_database.PassiveSkills);
        }

        private void AddCategory(IReadOnlyList<SkillDefinition> pool)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Count; i++)
            {
                SkillDefinition skill = pool[i];
                if (skill == null) continue;
                if (_loadout.Owns(skill))
                {
                    int lv = _loadout.LevelOf(skill);
                    if (lv >= skill.MaxLevel) continue;
                    SkillCard card = new SkillCard(skill, lv + 1, isNew: false);
                    _candidates.Add(card);
                }
                else
                {
                    if (_loadout.IsFull(skill.Category)) continue;
                    SkillCard card = new SkillCard(skill, 1, isNew: true);
                    _candidates.Add(card);
                }
            }
        }
    }
}
