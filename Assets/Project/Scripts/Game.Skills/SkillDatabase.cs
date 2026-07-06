using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Skills
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Configs/SkillDatabase")]
    public sealed class SkillDatabase : ScriptableObject
    {
        [SerializeField] private SkillDefinition[] activeSkills = Array.Empty<SkillDefinition>();
        [SerializeField] private SkillDefinition[] passiveSkills = Array.Empty<SkillDefinition>();

        public IReadOnlyList<SkillDefinition> ActiveSkills => activeSkills;
        public IReadOnlyList<SkillDefinition> PassiveSkills => passiveSkills;
    }
}
