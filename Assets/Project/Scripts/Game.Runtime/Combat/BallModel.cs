using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallModel : Observable
    {
        public Vector2 Position { get; private set; }

        public int BounceCount { get; private set; }

        public void Initialize(Vector2 origin)
        {
            Position = origin;
            BounceCount = 0;
            Raise();
        }

        public void SetPosition(Vector2 position)
        {
            if (Position == position) return;
            Position = position;
            Raise();
        }

        public void RegisterBounce(int count)
        {
            if (count <= 0) return;
            BounceCount += count;
        }
    }
}
