using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public interface IBallFactory
    {
        (BallModel model, BallView view) Create(string id, Vector2 origin, Vector2 direction, float speed, int baseDamage, BallSourceType sourceType);
        void Release(BallModel model, BallView view);
        string GenerateId();
    }
}
