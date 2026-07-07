using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public struct BallMotorStepResult
    {
        public int BounceCountThisStep;
        public int WallBounceCountThisStep;
        public bool HitFloor;
    }

    public readonly struct BallHit
    {
        public readonly Collider2D Collider;
        public readonly Vector2 Normal;
        public readonly Vector2 Point;
        public BallHit(Collider2D collider, Vector2 normal, Vector2 point)
        {
            Collider = collider;
            Normal = normal;
            Point = point;
        }
    }

    public interface IBallMotor
    {
        Vector2 Position { get; }
        IReadOnlyList<BallHit> LastStepHits { get; }
        void Init(Vector2 position, Vector2 direction, float speed, float radius, LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);
        BallMotorStepResult Step(float deltaTime);
    }
}
