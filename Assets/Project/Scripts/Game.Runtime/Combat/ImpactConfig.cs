using Game.Combat;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ImpactConfig", menuName = "Game/Configs/ImpactConfig")]
    public sealed class ImpactConfig : PoolConfiguration
    {
        [Header("Type")]
        [SerializeField] private BallSourceType sourceType = BallSourceType.Normal;

        public BallSourceType SourceType => sourceType;
    }
}
