using UnityEngine;

namespace Game.Runtime.Motor
{
    public struct BallMotorStepResult
    {
        public bool HitWall;
        public bool HitEnemy;
        public bool HitBlock;
        public bool PassedThrough;
        public Vector2 HitPoint;
        public Vector2 HitNormal;
        public Collider2D HitCollider;
        public int BounceCountThisStep;
        public bool StuckAborted;
    }

    public interface IBallMotor
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        float Speed { get; }
        void Init(Vector2 position, Vector2 direction, float speed, float radius, LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);
        BallMotorStepResult Step(float deltaTime);
    }
}
