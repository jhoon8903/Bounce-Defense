using Game.Combat;
using UnityEngine;

namespace Game.Events
{
    public readonly struct BallLaunchInfo
    {
        public readonly BallSourceType SourceType;
        public readonly Vector2 Position;
        public readonly Vector2 Direction;

        public BallLaunchInfo(BallSourceType sourceType, Vector2 position, Vector2 direction)
        {
            SourceType = sourceType;
            Position = position;
            Direction = direction;
        }
    }

    public readonly struct BallWallBounceInfo
    {
        public readonly int BallInstanceId;
        public readonly BallSourceType SourceType;
        public readonly Vector2 Position;
        public readonly Vector2 Normal;

        public BallWallBounceInfo(int ballInstanceId, BallSourceType sourceType, Vector2 position, Vector2 normal)
        {
            BallInstanceId = ballInstanceId;
            SourceType = sourceType;
            Position = position;
            Normal = normal;
        }
    }

    public readonly struct EnemyKillInfo
    {
        public readonly Vector2 Position;
        public readonly HitContext KillingHit;

        public EnemyKillInfo(Vector2 position, HitContext killingHit)
        {
            Position = position;
            KillingHit = killingHit;
        }
    }
}
