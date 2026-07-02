using System;
using UnityEngine;

namespace Game.Core.Pool
{
    public interface IPool
    {
        void Activate(Transform sceneRoot, params Type[] types);
        void Deactivate(params Type[] types);
        void DeactivateAll();
        T Get<T>() where T : PoolableView;
        void Return<T>(T instance) where T : PoolableView;
        void ReturnAll<T>() where T : PoolableView;
        void ReturnAllPools();
        void ClearPool<T>() where T : PoolableView;
    }
}
