using System.Collections.Generic;
using UnityEngine;

namespace Game.Skills
{
    // 드로우 단일 소스(플랜 §215). 액티브/패시브 풀 분리 → 캡 4/2 독립 규칙 지원.
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Configs/SkillDatabase")]
    public sealed class SkillDatabase : ScriptableObject
    {
        [SerializeField] private SkillDefinition[] activeSkills = new SkillDefinition[0];
        [SerializeField] private SkillDefinition[] passiveSkills = new SkillDefinition[0];

        public IReadOnlyList<SkillDefinition> ActiveSkills => activeSkills;
        public IReadOnlyList<SkillDefinition> PassiveSkills => passiveSkills;
    }
}
