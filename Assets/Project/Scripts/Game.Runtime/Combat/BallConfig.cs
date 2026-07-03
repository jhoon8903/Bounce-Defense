using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Game/Configs/BallConfig")]
    public sealed class BallConfig : PoolConfiguration
    {
        [Header("Motion")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float radius = 0.15f;

        [Header("Critical")]
        [SerializeField] [Range(0f, 1f)] private float critRate;
        [SerializeField] private float critDamage = 1.5f;

        [Header("Damage per Level (Lv1~3)")]
        [SerializeField] private float[] damagePerLevel = { 8f };

        // 효과(조건/lifeTime/Effect/중첩카운트/Damage) struct는 이후 개발 — 문서 참조

        public float Speed => speed;
        public float Radius => radius;
        public float CritRate => critRate;
        public float CritDamage => critDamage;
        public int MaxLevel => damagePerLevel.Length;

        public float GetDamage(int level)
        {
            if (damagePerLevel == null || damagePerLevel.Length == 0) return 0f;
            int index = Mathf.Clamp(level - 1, 0, damagePerLevel.Length - 1);
            return damagePerLevel[index];
        }
    }
}
