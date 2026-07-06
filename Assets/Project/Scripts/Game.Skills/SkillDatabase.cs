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
        [SerializeField] private SkillDefinition normalSkill; // 노멀 볼 — 결과창 집계 표시용(액티브/패시브 드래프트 풀엔 넣지 않음)

        public IReadOnlyList<SkillDefinition> ActiveSkills => activeSkills;
        public IReadOnlyList<SkillDefinition> PassiveSkills => passiveSkills;
        public SkillDefinition NormalSkill => normalSkill;

        // EffectKind로 스킬 정의 조회(액티브+패시브+노멀). 결과창 DTResult가 아이콘/이름을 여기서 얻는다(수동 매핑 없음).
        public SkillDefinition FindByEffectKind(SkillEffectKind kind)
        {
            if (kind == SkillEffectKind.None) return null;
            for (int i = 0; i < activeSkills.Length; i++)
                if (activeSkills[i] != null && activeSkills[i].EffectKind == kind) return activeSkills[i];
            for (int i = 0; i < passiveSkills.Length; i++)
                if (passiveSkills[i] != null && passiveSkills[i].EffectKind == kind) return passiveSkills[i];
            if (normalSkill != null && normalSkill.EffectKind == kind) return normalSkill;
            return null;
        }
    }
}
