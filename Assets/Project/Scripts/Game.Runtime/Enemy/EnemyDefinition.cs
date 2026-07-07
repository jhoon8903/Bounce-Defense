using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public enum BlockSize
    {
        Size1x1,
        Size1x2,
        Size2x1,
        Size2x2,
    }

    [CreateAssetMenu(fileName = "EnemyDef", menuName = "Game/Configs/EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Enemy";

        [Header("Body (Block Tile)")]
        [SerializeField] private BlockSize blockSize = BlockSize.Size1x1;
        [SerializeField] private Sprite blockSprite;

        [Header("Visual (Mob on top)")]
        [SerializeField] private Sprite mobSprite;

        [Header("Combat")]
        [SerializeField] [Min(1)] private int baseHp = 30;
        [SerializeField] [Min(0)] private int breachDamage = 10;

        [Header("Motion")]
        [SerializeField] [Min(0f)] private float descentSpeed = 0.5f;

        public string DisplayName => displayName;
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
