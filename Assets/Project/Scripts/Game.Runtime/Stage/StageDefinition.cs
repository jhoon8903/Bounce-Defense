using UnityEngine;

namespace Game.Runtime.Stage
{
    // 스테이지 = 웨이브 배열(개발플랜 §216). 과제 = 12 노멀 웨이브 + 성공 팝업(§48). 웨이브 곡선은 데이터 튜닝.
    [CreateAssetMenu(fileName = "StageDef", menuName = "Game/Configs/StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Stage 1";
        [SerializeField] [Min(1)] private int baseHp = 300; // §43 베이스 HP
        [SerializeField] private WaveDefinition[] waves;

        public string DisplayName => displayName;
        public int BaseHp => baseHp;
        public int WaveCount => waves != null ? waves.Length : 0;

        public WaveDefinition GetWave(int index) =>
            waves != null && index >= 0 && index < waves.Length ? waves[index] : null;
    }
}
