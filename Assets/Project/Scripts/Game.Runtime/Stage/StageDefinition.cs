using UnityEngine;

namespace Game.Runtime.Stage
{
    [CreateAssetMenu(fileName = "StageDef", menuName = "Game/Configs/StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Stage 1";
        [SerializeField] [Min(1)] private int baseHp = 300;
        [SerializeField] private WaveDefinition[] waves;

        [Header("Progression (킬 기반 XP 레벨업, 결정 B)")]
        [SerializeField] [Min(1)] private int xpPerKill = 1;
        [SerializeField] [Min(1)] private int baseXpToLevel = 5;
        [SerializeField] [Min(0)] private int xpGrowthPerLevel = 3;
        [SerializeField] [Min(1)] private int maxLevel = 19;

        [Header("Difficulty (웨이브별 적 HP 스케일)")]
        [SerializeField] [Min(0f)] private float hpGrowthPerWave = 0.15f;

        public int BaseHp => baseHp;
        public int WaveCount => waves?.Length ?? 0;
        public int XpPerKill => xpPerKill;
        public int BaseXpToLevel => baseXpToLevel;
        public int XpGrowthPerLevel => xpGrowthPerLevel;
        public int MaxLevel => maxLevel;

        public WaveDefinition GetWave(int index) => waves != null && index >= 0 && index < waves.Length ? waves[index] : null;

        public float WaveHpScale(int waveIndex) => 1f + Mathf.Max(0, waveIndex) * hpGrowthPerWave;
    }
}
