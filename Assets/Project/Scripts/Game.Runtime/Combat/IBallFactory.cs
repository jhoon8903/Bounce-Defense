using UnityEngine;

namespace Game.Runtime.Combat
{
    public interface IBallFactory
    {
        (BallModel model, BallView view) Create(Vector2 origin);
        void Release(BallModel model, BallView view);
        string GenerateId();
    }
}
