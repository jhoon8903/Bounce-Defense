using UnityEngine;

namespace Spike
{
    public sealed class EnemyStub : MonoBehaviour
    {
        public enum FacingMode { Moving, Frozen, BlockFixed }

        [SerializeField] FacingMode facingMode = FacingMode.Moving;
        [SerializeField] Vector2 patrolVelocity = Vector2.right * 2f;
        [SerializeField] Vector2 fixedForward = Vector2.down;

        Vector2 _lastMoveDir = Vector2.down;
        int _hitCount;

        public Vector2 Forward
        {
            get
            {
                if (facingMode == FacingMode.BlockFixed) return fixedForward.normalized;
                if (facingMode == FacingMode.Frozen) return _lastMoveDir;
                return patrolVelocity.sqrMagnitude > 0.0001f ? patrolVelocity.normalized : _lastMoveDir;
            }
        }

        void Update()
        {
            if (facingMode != FacingMode.Moving || patrolVelocity.sqrMagnitude <= 0.0001f) return;
            _lastMoveDir = patrolVelocity.normalized;
            transform.position += (Vector3)(patrolVelocity * Time.deltaTime);
        }

        public void OnBallHit(float ballSpeedAtHit)
        {
            _hitCount++;
            SpikeLog.Add($"Enemy {name} HIT#{_hitCount} ballSpeedAtHit={ballSpeedAtHit:F3}");
        }
    }
}
