using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public interface IBallFactory
    {
        (BallModel model, BallView view) Create(Vector2 origin, BallSourceType type);
        void Release(BallModel model, BallView view);
        string GenerateId();
    }
}
