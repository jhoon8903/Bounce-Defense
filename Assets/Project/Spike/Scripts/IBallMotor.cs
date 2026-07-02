using UnityEngine;

namespace Spike
{
    public struct BallMotorStepResult
    {
        public bool hitWall;
        public bool hitEnemy;
        public bool hitBlock;
        public bool passedThrough;
        public Vector2 hitPoint;
        public Vector2 hitNormal;
        public Collider2D hitCollider;
        public int bounceCountThisStep;
        public bool stuckAborted;
    }

    // Seam for Decision A: lets the Rigidbody2D hybrid candidate replace this later without touching launcher/damage code.
    public interface IBallMotor
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        float Speed { get; }

        void Init(Vector2 position, Vector2 direction, float speed, float radius,
            LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);

        BallMotorStepResult Step(float deltaTime);
    }
}
