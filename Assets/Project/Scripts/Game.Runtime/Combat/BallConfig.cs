using Game.Combat;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Game/Configs/BallConfig")]
    public sealed class BallConfig : PoolConfiguration
    {
        [Header("Type")]
        // 이 config(=타입별 프리팹)의 볼 타입. BallFactory가 타입별 풀 키로 사용(§11-9).
        [SerializeField] private BallSourceType sourceType = BallSourceType.Normal;

        [Header("Motion")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float radius = 0.15f;

        [Header("Damage per Level (Lv1~3)")]
        [SerializeField] private float[] damagePerLevel = { 8f };

        public BallSourceType SourceType => sourceType;
        public float Speed => speed;
        public float Radius => radius;

        public float GetDamage(int level)
        {
            if (damagePerLevel == null || damagePerLevel.Length == 0) return 0f;
            int index = Mathf.Clamp(level - 1, 0, damagePerLevel.Length - 1);
            return damagePerLevel[index];
        }
    }
}
