using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public interface IBallFactory
    {
        // 볼 타입별 풀에서 해당 타입 프리팹을 꺼낸다(등록 안 된 타입은 폴백=Normal).
        (BallModel model, BallView view) Create(Vector2 origin, BallSourceType type);
        void Release(BallModel model, BallView view);
        string GenerateId();
    }
}
