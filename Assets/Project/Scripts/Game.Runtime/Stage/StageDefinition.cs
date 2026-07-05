using UnityEngine;

namespace Game.Runtime.Stage
{
    // 스테이지 = 웨이브 배열(개발플랜 §216). 과제 = 12 노멀 웨이브 + 성공 팝업(§48). 웨이브 곡선은 데이터 튜닝.
    [CreateAssetMenu(fileName = "StageDef", menuName = "Game/Configs/StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Stage 1"; // 에셋 식별 메모용(런타임 미사용)
        [SerializeField] [Min(1)] private int baseHp = 300; // §43 베이스 HP
        [SerializeField] private WaveDefinition[] waves;

        [Header("Progression (킬 기반 XP 레벨업, 결정 B)")]
        [SerializeField] [Min(1)] private int xpPerKill = 1;        // 킬당 XP
        [SerializeField] [Min(1)] private int baseXpToLevel = 5;    // Lv1→2 필요 XP(≈킬 수)
        [SerializeField] [Min(0)] private int xpGrowthPerLevel = 3; // 레벨마다 임계 증가(선형)

        public int BaseHp => baseHp;
        public int WaveCount => waves != null ? waves.Length : 0;
        public int XpPerKill => xpPerKill;
        public int BaseXpToLevel => baseXpToLevel;
        public int XpGrowthPerLevel => xpGrowthPerLevel;

        public WaveDefinition GetWave(int index) =>
            waves != null && index >= 0 && index < waves.Length ? waves[index] : null;
    }
}
