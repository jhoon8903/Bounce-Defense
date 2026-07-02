using UnityEngine;

namespace Spike
{
    [DisallowMultipleComponent]
    public sealed class SpikeBall : MonoBehaviour
    {
        [SerializeField] float speed = 12f;
        [SerializeField] float radius = 0.15f;
        [SerializeField] float lifetime = 8f;
        [SerializeField] int maxBounces = 40;
        [SerializeField] bool isGhost;
        [SerializeField] LayerMask wallMask;
        [SerializeField] LayerMask enemyMask;
        [SerializeField] LayerMask blockMask;
        [SerializeField] Vector2 outOfBoundsMin = new Vector2(-6f, -11f);
        [SerializeField] Vector2 outOfBoundsMax = new Vector2(6f, 11f);

        const int SameColliderStuckThreshold = 30;

        IBallMotor _motor;
        float _age;
        int _bounceCount;
        bool _ended;
        Collider2D _lastTouchedCollider;
        int _sameColliderStreak;
        float _nextHeartbeatAt;

        public bool Ended => _ended;
        public int BounceCount => _bounceCount;
        public float CurrentSpeed => _motor?.Speed ?? 0f;

        public void SetGhost(bool ghost) => isGhost = ghost;

        public void Launch(Vector2 origin, Vector2 direction)
        {
            _motor = new KinematicRaycastMotor();
            int passThroughValue = isGhost ? (enemyMask.value | blockMask.value) : 0;
            _motor.Init(origin, direction, speed, radius, wallMask, enemyMask, blockMask, passThroughValue);
            transform.position = origin;
            _age = 0f;
            _bounceCount = 0;
            _ended = false;
            _lastTouchedCollider = null;
            _sameColliderStreak = 0;
            _nextHeartbeatAt = 1f;
        }

        // Fixed-step driven (not Update/deltaTime) so results are reproducible regardless of
        // editor render framerate, and so a real corner/groove stuck loop shows up as many
        // fixed-steps re-touching the same collider rather than as unbounded render-frame spam.
        void FixedUpdate()
        {
            if (_motor == null || _ended) return;

            float dt = Time.fixedDeltaTime;
            _age += dt;
            var result = _motor.Step(dt);
            transform.position = _motor.Position;
            _bounceCount += result.bounceCountThisStep;

            var pos = _motor.Position;
            if (pos.x < outOfBoundsMin.x || pos.x > outOfBoundsMax.x || pos.y < outOfBoundsMin.y || pos.y > outOfBoundsMax.y)
            {
                SpikeLog.Add($"{name} OUT_OF_BOUNDS_ESCAPE pos={pos} vel={_motor.Velocity} age={_age:F2}s");
                EndTurn("out_of_bounds");
                return;
            }

            bool touched = result.bounceCountThisStep > 0 || result.passedThrough;
            if (touched)
            {
                if (result.hitCollider != _lastTouchedCollider)
                {
                    HandleHit(result);
                    _lastTouchedCollider = result.hitCollider;
                    _sameColliderStreak = 1;
                }
                else
                {
                    _sameColliderStreak++;
                    if (_sameColliderStreak == SameColliderStuckThreshold)
                    {
                        SpikeLog.Add($"{name} STUCK_ON_SAME_COLLIDER {result.hitCollider.name} streak={_sameColliderStreak} t={_age:F2}s");
                    }
                }
            }
            else
            {
                _lastTouchedCollider = null;
                _sameColliderStreak = 0;
            }

            if (result.stuckAborted)
            {
                SpikeLog.Add($"{name} ANTI_STUCK t={_age:F2}s pos={_motor.Position}");
            }

            if (_age >= _nextHeartbeatAt)
            {
                _nextHeartbeatAt += 1f;
                SpikeLog.Add($"{name} HEARTBEAT t={_age:F2}s pos={_motor.Position} vel={_motor.Velocity}");
            }

            if (_age >= lifetime)
            {
                EndTurn("lifetime");
            }
            else if (_bounceCount >= maxBounces)
            {
                EndTurn("bounceCap");
            }
        }

        void HandleHit(BallMotorStepResult result)
        {
            string kind = result.hitWall ? "Wall" : result.hitBlock ? "Block" : result.hitEnemy ? "Enemy" : "Unknown";
            string mode = result.passedThrough ? "PASS" : "BOUNCE";
            string zone = "-";

            var enemy = result.hitCollider != null ? result.hitCollider.GetComponent<EnemyStub>() : null;
            if (enemy != null)
            {
                zone = HitZoneClassifier.Classify(enemy.Forward, result.hitNormal).ToString();
                if (!result.passedThrough)
                {
                    enemy.OnBallHit(_motor.Speed);
                }
            }

            SpikeLog.Add($"{name} {mode} {kind} zone={zone} speed={_motor.Speed:F3} pos={result.hitPoint}");
        }

        void EndTurn(string reason)
        {
            _ended = true;
            SpikeLog.Add($"{name} TURN_END reason={reason} bounces={_bounceCount} age={_age:F2}s");
        }
    }
}
