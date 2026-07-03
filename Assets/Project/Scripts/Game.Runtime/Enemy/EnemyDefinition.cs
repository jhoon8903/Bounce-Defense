using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 인스펙터에서 고르는 블록 풋프린트(셀 WxH). Footprint 구조체는 직렬화가 안 돼 enum으로 노출.
    public enum BlockSize
    {
        Size1x1,
        Size1x2, // 1열 x 2행
        Size2x1, // 2열 x 1행
        Size2x2,
    }

    // 적 '종류' 데이터(데이터주도, 결정 D). 개발플랜 §216: EnemyDefinition { hp; speed; footprint } + 비주얼.
    // 적 = 돌 타일(블록, 콜라이더 몸체) + 그 위 몹 스프라이트. 이 SO가 한 종류의 전부를 정의.
    [CreateAssetMenu(fileName = "EnemyDef", menuName = "Game/Configs/EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Enemy";

        [Header("Body (Block Tile)")]
        [SerializeField] private BlockSize blockSize = BlockSize.Size1x1;
        [SerializeField] private Sprite blockSprite; // 돌 타일(콜라이더 크기 = footprint*cellSize)

        [Header("Visual (Mob on top)")]
        [SerializeField] private Sprite mobSprite;   // 블록 위에 올라가는 몬스터

        [Header("Combat")]
        [SerializeField] [Min(1)] private int baseHp = 30;
        [SerializeField] [Min(0)] private int breachDamage = 10; // 방어선 도달 시 베이스 HP에 주는 피해

        [Header("Motion")]
        [SerializeField] [Min(0f)] private float descentSpeed = 0.5f; // 초당 월드 유닛 하강

        public string DisplayName => displayName;
        public BlockSize BlockSize => blockSize;
        public Sprite BlockSprite => blockSprite;
        public Sprite MobSprite => mobSprite;
        public int BaseHp => baseHp;
        public int BreachDamage => breachDamage;
        public float DescentSpeed => descentSpeed;

        public Footprint Footprint => ToFootprint(blockSize);

        public static Footprint ToFootprint(BlockSize size)
        {
            switch (size)
            {
                case BlockSize.Size1x2: return Footprint.Size1x2;
                case BlockSize.Size2x1: return Footprint.Size2x1;
                case BlockSize.Size2x2: return Footprint.Size2x2;
                default: return Footprint.Size1x1;
            }
        }
    }
}
