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

    // 적이 하단 방어선에 도달(=베이스 침범). 웨이브 진행상 킬처럼 '해소'로 집계 + 베이스 HP 감소.
    public readonly struct EnemyBreachInfo
    {
        public readonly int BreachDamage;
        public readonly Vector2 Position;

        public EnemyBreachInfo(int breachDamage, Vector2 position)
        {
            BreachDamage = breachDamage;
            Position = position;
        }
    }
}
