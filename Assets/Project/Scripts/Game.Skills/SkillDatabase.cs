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
        [SerializeField] private SkillDefinition normalSkill;

        public IReadOnlyList<SkillDefinition> ActiveSkills => activeSkills;
        public IReadOnlyList<SkillDefinition> PassiveSkills => passiveSkills;
        public SkillDefinition NormalSkill => normalSkill;

        public SkillDefinition FindByEffectKind(SkillEffectKind kind)
        {
            if (kind == SkillEffectKind.None) return null;
            for (int i = 0; i < activeSkills.Length; i++)
            {
                if (activeSkills[i] != null && activeSkills[i].EffectKind == kind) return activeSkills[i];
            }
            for (int i = 0; i < passiveSkills.Length; i++)
            {
                if (passiveSkills[i] != null && passiveSkills[i].EffectKind == kind) return passiveSkills[i];
            }
            if (normalSkill != null && normalSkill.EffectKind == kind) return normalSkill;
            return null;
        }
    }
}
