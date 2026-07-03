using Game.Combat;
using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallModel : Observable
    {
        private string _id;
        private Vector2 _position;
        private Vector2 _direction = Vector2.up;
        private float _speed;
        private int _baseDamage;
        private BallSourceType _sourceType;
        private int _bounceCount;
        private bool _isActive;

        public string Id => _id;
        public Vector2 Position => _position;
        public Vector2 Direction => _direction;
        public float Speed => _speed;
        public int BaseDamage => _baseDamage;
        public BallSourceType SourceType => _sourceType;
        public int BounceCount => _bounceCount;
        public bool IsActive => _isActive;

        public void Initialize(string id, Vector2 origin, Vector2 direction, float speed, int baseDamage, BallSourceType sourceType)
        {
            _id = id;
            _position = origin;
            _direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.up;
            _speed = speed;
            _baseDamage = baseDamage;
            _sourceType = sourceType;
            _bounceCount = 0;
            _isActive = true;
            Raise();
        }

        public void SetPosition(Vector2 position)
        {
            if (_position == position) return;
            _position = position;
            Raise();
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon) return;
            Vector2 normalized = direction.normalized;
            if (_direction == normalized) return;
            _direction = normalized;
            Raise();
        }

        // 바운스 카운트는 렌더 상태가 아니므로 Raise 생략 (컨트롤러 종료 판정용)
        public void RegisterBounce(int count)
        {
            if (count <= 0) return;
            _bounceCount += count;
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            Raise();
        }
    }
}
