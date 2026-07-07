using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallModel : Observable
    {
        private Vector2 _position;
        private int _bounceCount;

        public Vector2 Position => _position;
        public int BounceCount => _bounceCount;

        public void Initialize(Vector2 origin)
        {
            _position = origin;
            _bounceCount = 0;
            Raise();
        }

        public void SetPosition(Vector2 position)
        {
            if (_position == position) return;
            _position = position;
            Raise();
        }

        public void RegisterBounce(int count)
        {
            if (count <= 0) return;
            _bounceCount += count;
        }
    }
}
