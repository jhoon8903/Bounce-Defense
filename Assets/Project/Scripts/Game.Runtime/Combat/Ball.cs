using Game.Combat;
using Game.Core.Pool;
using Game.Events;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class Ball : PoolableView
    {
        [SerializeField] private float radius = 0.15f;
        [SerializeField] private float lifetime = 8f;
        [SerializeField] private int maxBounces = 40;
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private LayerMask blockMask;
        [SerializeField] private Vector2 outOfBoundsMin = new(-6f, -11f);
        [SerializeField] private Vector2 outOfBoundsMax = new(6f, 11f);

        private const int SameColliderStuckThreshold = 30;
        private IBallMotor _motor;
        private DamageResolver _resolver;
        private CombatEventHub _hub;
        private IPool _pool;
        private BallSourceType _sourceType;
        private int _baseDamage;
        private float _age;
        private int _bounceCount;
        private bool _ended;
        private Collider2D _lastTouchedCollider;
        private int _sameColliderStreak;
        public bool Ended => _ended;
        
        public void Configure(DamageResolver resolver, CombatEventHub hub, IPool pool)
        {
            _resolver = resolver;
            _hub = hub;
            _pool = pool;
        }

        public void Launch(Vector2 origin, Vector2 direction, float speed, int baseDamage, BallSourceType sourceType)
        {
            _sourceType = sourceType;
            _baseDamage = baseDamage;
            _motor = new KinematicRaycastMotor();
            _motor.Init(origin, direction, speed, radius, wallMask, enemyMask, blockMask, passThroughMask: 0);
            transform.position = origin;
            _age = 0f;
            _bounceCount = 0;
            _ended = false;
            _lastTouchedCollider = null;
            _sameColliderStreak = 0;
            _hub?.RaiseLaunch(new BallLaunchInfo(sourceType, origin, direction));
        }

        private void FixedUpdate()
        {
            if (_motor == null || _ended) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;
            BallMotorStepResult result = _motor.Step(dt);
            transform.position = _motor.Position;
            _bounceCount += result.BounceCountThisStep;
            Vector2 pos = _motor.Position;
            if (pos.x < outOfBoundsMin.x || pos.x > outOfBoundsMax.x || pos.y < outOfBoundsMin.y || pos.y > outOfBoundsMax.y)
            {
                EndTurn();
                return;
            }

            bool touched = result.BounceCountThisStep > 0;
            if (touched)
            {
                if (result.HitCollider != _lastTouchedCollider)
                {
                    HandleHit(result);
                    _lastTouchedCollider = result.HitCollider;
                    _sameColliderStreak = 1;
                }
                else _sameColliderStreak++;
                if (_sameColliderStreak >= SameColliderStuckThreshold)
                {
                    EndTurn();
                    return;
                }
            }
            else
            {
                _lastTouchedCollider = null;
                _sameColliderStreak = 0;
            }

            if (_age >= lifetime || _bounceCount >= maxBounces) EndTurn();
        }

        private void HandleHit(BallMotorStepResult result)
        {
            if (result.HitWall)
            {
                _hub?.RaiseWallBounce(new BallWallBounceInfo(gameObject.GetInstanceID(), _sourceType, result.HitPoint, result.HitNormal));
                return;
            }

            if (!result.HitEnemy || result.HitCollider == null || !result.HitCollider.TryGetComponent<IDamageable>(out var damageable)) return;
            HitContext context = new HitContext(
                DamageType.Direct,
                _sourceType,
                damageable,
                _baseDamage,
                canCrit: true,
                canReceiveGlobalModifiers: true,
                result.HitPoint,
                result.HitNormal);

            _resolver?.Resolve(context);
            _hub?.RaiseHit(context);
        }

        private void EndTurn()
        {
            _ended = true;
            _pool?.Return(this);
        }

        public override void OnInactive()
        {
            base.OnInactive();
            _motor = null;
        }
    }
}
