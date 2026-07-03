using UnityEngine;

namespace Game.Runtime.Enemy
{
    public interface IEnemyFactory
    {
        (EnemyModel model, EnemyView view) Create(string id, EnemyDefinition definition, Vector2 position);
        void Release(EnemyModel model, EnemyView view);
        string GenerateId();
    }
}
