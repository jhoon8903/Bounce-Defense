using System.Collections.Generic;
using Game.Core.Random;
using Game.Skills;

namespace Game.Roguelike
{
    // 3택 드로우 규칙(스펙 §3 + 플랜 §260):
    //  - 통합 풀에서 후보 생성, SO 참조당 ≤1장 → 자동 무중복(동일 스킬 카드 안 나옴).
    //  - 미보유 & 카테고리 캡 미달 → 신규(Lv1) 후보.
    //  - 보유 & 만렙 미만 → 업그레이드(보유+1) 후보.
    //  - 보유 만렙 → 제외. 미보유 & 캡 도달 → 제외(신규 불가 → 보유 업그레이드만 남음).
    //  - 후보 셔플(IRandom 시드) 후 최대 count장. 유효 후보 < count면 있는 만큼(§261 <3 폴백).
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

        // 유효 후보 수(디버그/드래프트 스킵 판정용).
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
            for (int i = 0; i < n; i++) result.Add(_candidates[i]);
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
                    if (lv < skill.MaxLevel)
                        _candidates.Add(new SkillCard(skill, lv + 1, isNew: false)); // 업그레이드
                    // 만렙 → 제외
                }
                else
                {
                    if (!_loadout.IsFull(skill.Category))
                        _candidates.Add(new SkillCard(skill, 1, isNew: true)); // 신규 Lv1
                    // 캡 도달 → 신규 제외
                }
            }
        }
    }
}
