using System.Collections.Generic;
using Game.Core.Observer;
using Game.Skills;

namespace Game.Roguelike
{
    // 보유 스킬 명단(스펙 §3: 액티브 캡 4 / 패시브 캡 2, 독립). Observable → SkillMonitor(로드아웃 UI)가 구독.
    // 키 = SkillDefinition 참조(SO가 곧 정체성 — 문자열 오타 위험 없음, 참조 동등성으로 무중복).
    public sealed class PlayerLoadout : Observable
    {
        public const int ActiveCap = 4;
        public const int PassiveCap = 2;

        private readonly Dictionary<SkillDefinition, int> _levels = new();
        private readonly List<SkillDefinition> _order = new(); // 획득 순서(로드아웃 슬롯 안정 표시)
        private int _activeCount;
        private int _passiveCount;

        public int ActiveCount => _activeCount;
        public int PassiveCount => _passiveCount;

        public bool Owns(SkillDefinition skill) => skill != null && _levels.ContainsKey(skill);

        public int LevelOf(SkillDefinition skill) =>
            skill != null && _levels.TryGetValue(skill, out int lv) ? lv : 0;

        public bool IsFull(SkillCategory category) =>
            category == SkillCategory.Active ? _activeCount >= ActiveCap : _passiveCount >= PassiveCap;

        // 신규 획득(Lv1). 이미 보유/캡 초과면 무시(드로우 규칙이 선제 보장하지만 방어).
        public void Acquire(SkillDefinition skill)
        {
            if (skill == null || _levels.ContainsKey(skill)) return;
            if (IsFull(skill.Category)) return;
            _levels[skill] = 1;
            _order.Add(skill);
            if (skill.Category == SkillCategory.Active) _activeCount++; else _passiveCount++;
            Raise();
        }

        // 보유 스킬 1레벨 상승(만렙 클램프).
        public void Upgrade(SkillDefinition skill)
        {
            if (skill == null || !_levels.TryGetValue(skill, out int lv)) return;
            if (lv >= skill.MaxLevel) return;
            _levels[skill] = lv + 1;
            Raise();
        }

        // 카드 적용: 신규면 Acquire, 보유면 Upgrade.
        public void Apply(SkillCard card)
        {
            if (!card.IsValid) return;
            if (card.IsNew) Acquire(card.Definition);
            else Upgrade(card.Definition);
        }

        // 카테고리별 보유 목록을 획득 순서대로 버퍼에 채운다(UI 슬롯 안정, 할당 없음).
        public void CopyOwned(SkillCategory category, List<KeyValuePair<SkillDefinition, int>> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _order.Count; i++)
            {
                SkillDefinition s = _order[i];
                if (s != null && s.Category == category && _levels.TryGetValue(s, out int lv))
                    buffer.Add(new KeyValuePair<SkillDefinition, int>(s, lv));
            }
        }

        // 재시작(§264): 스킬 0으로.
        public void ResetLoadout()
        {
            _levels.Clear();
            _order.Clear();
            _activeCount = 0;
            _passiveCount = 0;
            Raise();
        }
    }
}
