using Game.Combat;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Game/Configs/BallConfig")]
    public sealed class BallConfig : PoolConfiguration
    {
        [Header("Type")]
        [SerializeField] private BallSourceType sourceType = BallSourceType.Normal;

        [Header("Motion")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float radius = 0.15f;

        public BallSourceType SourceType => sourceType;
        public float Speed => speed;
        public float Radius => radius;
    }
}
