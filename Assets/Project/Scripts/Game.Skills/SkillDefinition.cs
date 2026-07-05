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
        // 런타임 동작 식별자(Phase 4). SkillModuleFactory가 이 값으로 볼 타입/모듈/패시브 모디파이어를 만든다.
        // None = 아직 동작 미배정(카드 표시만). 액티브는 FireBall 등, 패시브는 WarmTin 등으로 인스펙터에서 지정.
        [SerializeField] private SkillEffectKind effectKind = SkillEffectKind.None;
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(1)] private int maxLevel = 3;

        [Header("Per-level display (index 0 = Lv1, UI는 1-index)")]
        [SerializeField] [TextArea] private string[] descriptionPerLevel = new string[3];
        // 액티브 = ★볼데미지(레벨별, 스펙 §4). 패시브는 비워둠 → 카드에서 ★ 숨김.
        [SerializeField] private int[] ballDamagePerLevel = new int[0];

        // 액티브 볼의 2차효과 수치(§11-17 결정: 인스펙터 튜닝). 자기 effectKind가 쓰는 섹션만 채운다.
        // 모듈(FireBall/IceBall 등)이 GetXxx(level)로 읽어간다 — 패시브 %는 in-module 유지(볼 아님).
        [Header("Effect params (자기 효과 섹션만 채움)")]
        [SerializeField] private BurnEffect burn = new BurnEffect();       // FireBall
        [SerializeField] private FreezeEffect freeze = new FreezeEffect(); // IceBall
        [SerializeField] private LaserEffect laser = new LaserEffect();     // LaserBall
        [SerializeField] private ClusterEffect cluster = new ClusterEffect(); // ClusterBall
        [SerializeField] private MirrorEffect mirror = new MirrorEffect();     // MagicMirror(패시브)
        [SerializeField] private LastMatchEffect lastMatch = new LastMatchEffect(); // LastMatch(패시브)

        // Fire 번(초당 틱): 지속·dps·최대중첩(플랜 §198). durationPerLevel 등 index 0 = Lv1.
        [System.Serializable]
        public sealed class BurnEffect
        {
            public float[] durationPerLevel = new float[0];
            public float[] dpsPerLevel = new float[0];
            public int[] maxStacksPerLevel = new int[0];
        }

        // Ice 냉동: 발동확률·지속·하강슬로우 + Ice볼 상시 추가뎀 가산%(플랜 §199).
        [System.Serializable]
        public sealed class FreezeEffect
        {
            public float[] chancePerLevel = new float[0];              // 0.30 = 30%
            public float[] durationPerLevel = new float[0];            // 초
            public float[] slowPerLevel = new float[0];                // 0.20 = 하강 20% 감속
            public float[] bonusDamagePercentPerLevel = new float[0];  // 0.10 = Ice볼 상시 +10%
        }

        // Laser 행뎀: 같은 행 다른 적에 flat(플랜 §200).
        [System.Serializable]
        public sealed class LaserEffect
        {
            public float[] rowDamagePerLevel = new float[0];
        }

        // Cluster 분열: 발동확률 + 특수볼 데미지(플랜 §202).
        [System.Serializable]
        public sealed class ClusterEffect
        {
            public float[] chancePerLevel = new float[0];         // 0.40 = 40%
            public float[] specialDamagePerLevel = new float[0];  // 특수볼 직격 데미지
        }

        // Magic Mirror(패시브): 벽튕김 후 다음 직격 소비형 가산%(플랜 §209).
        [System.Serializable]
        public sealed class MirrorEffect
        {
            public float[] bonusPercentPerLevel = new float[0];   // 0.20 = +20%
        }

        // Last Match(패시브): OnKill 반경 폭발 데미지 + 반경(플랜 §212).
        [System.Serializable]
        public sealed class LastMatchEffect
        {
            public float[] explosionDamagePerLevel = new float[0]; // flat 폭발뎀
            public float[] radiusPerLevel = new float[0];          // 월드 반경
        }

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public SkillCategory Category => category;
        public SkillEffectKind EffectKind => effectKind;
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

        // ---- 2차효과 수치 접근(레벨 클램프, 미설정 시 0) ----
        // Fire 번
        public float GetBurnDuration(int level) => Sample(burn?.durationPerLevel, level);
        public float GetBurnDps(int level) => Sample(burn?.dpsPerLevel, level);
        public int GetBurnMaxStacks(int level) => SampleInt(burn?.maxStacksPerLevel, level);
        // Ice 냉동
        public float GetFreezeChance(int level) => Sample(freeze?.chancePerLevel, level);
        public float GetFreezeDuration(int level) => Sample(freeze?.durationPerLevel, level);
        public float GetFreezeSlow(int level) => Sample(freeze?.slowPerLevel, level);
        public float GetIceBonusPercent(int level) => Sample(freeze?.bonusDamagePercentPerLevel, level);
        // Laser 행뎀
        public float GetLaserRowDamage(int level) => Sample(laser?.rowDamagePerLevel, level);
        // Cluster 분열
        public float GetClusterChance(int level) => Sample(cluster?.chancePerLevel, level);
        public float GetClusterSpecialDamage(int level) => Sample(cluster?.specialDamagePerLevel, level);
        // Magic Mirror
        public float GetMirrorBonus(int level) => Sample(mirror?.bonusPercentPerLevel, level);
        // Last Match
        public float GetLastMatchDamage(int level) => Sample(lastMatch?.explosionDamagePerLevel, level);
        public float GetLastMatchRadius(int level) => Sample(lastMatch?.radiusPerLevel, level);

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
