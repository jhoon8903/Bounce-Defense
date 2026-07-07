using System;
using UnityEngine;

namespace Game.Skills
{
    [CreateAssetMenu(fileName = "SkillDef", menuName = "Game/Configs/SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_id";
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private SkillCategory category = SkillCategory.Active;
        [SerializeField] private SkillEffectKind effectKind = SkillEffectKind.None;
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(1)] private int maxLevel = 3;

        [Header("Per-level display (index 0 = Lv1, UI는 1-index)")]
        [SerializeField] [TextArea] private string[] descriptionPerLevel = new string[3];
        [SerializeField] private int[] ballDamagePerLevel = Array.Empty<int>();

        [Header("Effect params (자기 효과 섹션만 채움)")]
        [SerializeField] private BurnEffect burn = new();
        [SerializeField] private FreezeEffect freeze = new();
        [SerializeField] private LaserEffect laser = new();
        [SerializeField] private ClusterEffect cluster = new();
        [SerializeField] private MirrorEffect mirror = new();
        [SerializeField] private LastMatchEffect lastMatch = new();

        #region Active Skill

        #region Active Fire (Burn)

        [Serializable]
        public sealed class BurnEffect
        {
            public float[] durationPerLevel = Array.Empty<float>();
            public float[] dpsPerLevel = Array.Empty<float>();
            public int[] maxStacksPerLevel = Array.Empty<int>();
        }
        
        public float GetBurnDuration(int level) => Sample(burn?.durationPerLevel, level);
        public float GetBurnDps(int level) => Sample(burn?.dpsPerLevel, level);
        public int GetBurnMaxStacks(int level) => SampleInt(burn?.maxStacksPerLevel, level);

        #endregion

        #region Active Ice

        [Serializable]
        public sealed class FreezeEffect
        {
            public float[] chancePerLevel = Array.Empty<float>();
            public float[] durationPerLevel = Array.Empty<float>();
            public float[] slowPerLevel = Array.Empty<float>();
            public float[] bonusDamagePercentPerLevel = Array.Empty<float>();
        }
        
        public float GetFreezeChance(int level) => Sample(freeze?.chancePerLevel, level);
        public float GetFreezeDuration(int level) => Sample(freeze?.durationPerLevel, level);
        public float GetFreezeSlow(int level) => Sample(freeze?.slowPerLevel, level);
        public float GetIceBonusPercent(int level) => Sample(freeze?.bonusDamagePercentPerLevel, level);

        #endregion

        #region Active Laser

        [Serializable]
        public sealed class LaserEffect
        {
            public float[] rowDamagePerLevel = Array.Empty<float>();
        }
        
        public float GetLaserRowDamage(int level) => Sample(laser?.rowDamagePerLevel, level);

        #endregion
        
        #region Active Cluster
        
        [Serializable]
        public sealed class ClusterEffect
        {
            public float[] chancePerLevel = Array.Empty<float>();
            public float[] specialDamagePerLevel = Array.Empty<float>();
        }
        
        public float GetClusterChance(int level) => Sample(cluster?.chancePerLevel, level);
        public float GetClusterSpecialDamage(int level) => Sample(cluster?.specialDamagePerLevel, level);
        
        #endregion

        #endregion

        #region Passive Skill

        #region Passive Mirror

        [Serializable]
        public sealed class MirrorEffect
        {
            public float[] bonusPercentPerLevel = Array.Empty<float>();
        }
        
        public float GetMirrorBonus(int level) => Sample(mirror?.bonusPercentPerLevel, level);

        #endregion

        #region Passive Last Match

        [Serializable]
        public sealed class LastMatchEffect
        {
            public float[] explosionDamagePerLevel = Array.Empty<float>();
            public float[] radiusPerLevel = Array.Empty<float>();
        }
        
        public float GetLastMatchDamage(int level) => Sample(lastMatch?.explosionDamagePerLevel, level);
        public float GetLastMatchRadius(int level) => Sample(lastMatch?.radiusPerLevel, level);

        #endregion

        #endregion
        
        public string SkillId => skillId;
        public string DisplayName => displayName;
        public SkillCategory Category => category;
        public SkillEffectKind EffectKind => effectKind;
        public Sprite Icon => icon;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public bool HasBallDamage => ballDamagePerLevel is { Length: > 0 };
        
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

        private static float Sample(float[] arr, int level)
        {
            if (arr == null || arr.Length == 0) return 0f;
            return arr[Mathf.Clamp(level - 1, 0, arr.Length - 1)];
        }

        private static int SampleInt(int[] arr, int level)
        {
            if (arr == null || arr.Length == 0) return 0;
            return arr[Mathf.Clamp(level - 1, 0, arr.Length - 1)];
        }
    }
}
