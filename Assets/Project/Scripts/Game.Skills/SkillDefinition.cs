using UnityEngine;

namespace Game.Skills
{
    // 한 스킬의 정적 데이터(데이터주도, 플랜 §189). Phase 3 = 카드 표시/드로우에 필요한 데이터만.
    // 스킬의 '동작'(번/냉동/크리 등 IBallModule/IPassiveModule)은 Phase 4 확장점 — 여기엔 아직 없음(YAGNI).
    // 정체성 = 이 SO 참조 자체(로드아웃/드로우가 참조 동등성으로 무중복 보장). skillId는 표시/디버그용.
    [CreateAssetMenu(fileName = "SkillDef", menuName = "Game/Configs/SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_id";
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private SkillCategory category = SkillCategory.Active;
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(1)] private int maxLevel = 3;

        [Header("Per-level display (index 0 = Lv1, UI는 1-index)")]
        [SerializeField] [TextArea] private string[] descriptionPerLevel = new string[3];
        // 액티브 = ★볼데미지(레벨별, 스펙 §4). 패시브는 비워둠 → 카드에서 ★ 숨김.
        [SerializeField] private int[] ballDamagePerLevel = new int[0];

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public SkillCategory Category => category;
        public Sprite Icon => icon;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public bool HasBallDamage => ballDamagePerLevel != null && ballDamagePerLevel.Length > 0;

        // level = 1-index. 배열 = 0-index. 범위 밖 클램프(만렙 카드 표시 안전).
        public string GetDescription(int level)
        {
            if (descriptionPerLevel == null || descriptionPerLevel.Length == 0) return string.Empty;
            int i = Mathf.Clamp(level - 1, 0, descriptionPerLevel.Length - 1);
            return descriptionPerLevel[i] ?? string.Empty;
        }

        public int GetBallDamage(int level)
        {
            if (!HasBallDamage) return 0;
            int i = Mathf.Clamp(level - 1, 0, ballDamagePerLevel.Length - 1);
            return ballDamagePerLevel[i];
        }
    }
}
