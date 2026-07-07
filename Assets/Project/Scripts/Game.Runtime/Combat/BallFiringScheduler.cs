using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallFiringScheduler
    {
        private const float FireInterval = 0.12f;

        private bool _firing;
        private Vector2 _direction = Vector2.up;
        private float _timer;

        public Vector2 Direction => _direction;

        public void Start(Vector2 direction)
        {
            SetDirection(direction);
            _firing = true;
            _timer = FireInterval;
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon) _direction = direction.normalized;
        }

        public bool TryFire(float deltaTime, int inFlight, int maxInFlight)
        {
            if (!_firing) return false;
            _timer += deltaTime;
            if (_timer < FireInterval) return false;
            if (inFlight >= maxInFlight)
            {
                _timer = FireInterval;
                return false;
            }
            _timer = 0f;
            return true;
        }
    }
}
