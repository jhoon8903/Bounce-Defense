using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Base;
using UnityEngine;

namespace Game.Core.Pool
{
    public sealed class GamePool : IPool
    {
        private readonly Dictionary<Type, PoolConfiguration> _poolConfigs = new();
        private readonly Dictionary<Type, object> _pools = new();
        private readonly Dictionary<Type, Transform> _poolParents = new();

        private Transform _activeSceneRoot;

        public GamePool(IEnumerable<PoolConfiguration> configs)
        {
            foreach (PoolConfiguration config in configs ?? Array.Empty<PoolConfiguration>())
            {
                if (!config)
                {
                    Verbose.W("[GamePool] Null config found in configs. Skipping.");
                    continue;
                }

                Type type = config.GetPoolType();
                if (type == null)
                {
                    Verbose.W($"[GamePool] Could not determine type for config '{config.name}'. Skipping.");
                    continue;
                }

                if (!_poolConfigs.TryAdd(type, config))
                {
                    Verbose.W($"[GamePool] Duplicate config for type '{type.Name}'. Using first occurrence.");
                }
            }
        }

        public void Activate(Transform sceneRoot, params Type[] types)
        {
            if (!sceneRoot)
            {
                Verbose.E("[GamePool] Scene root Transform is null.");
                return;
            }

            _activeSceneRoot = sceneRoot;

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null) continue;
                if (!_poolConfigs.TryGetValue(type, out PoolConfiguration config))
                {
                    Verbose.E($"[GamePool] No PoolConfig found for type '{type.Name}'. Did you add it to configs?");
                    continue;
                }

                if (_pools.ContainsKey(type)) continue;
                CreatePool(type, config);
            }
        }

        public void Deactivate(params Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || !_pools.ContainsKey(type)) continue;
                DestroyPool(type);
            }
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < _pools.Keys.ToArray().Length; i++)
            {
                Type type = _pools.Keys.ToArray()[i];
                DestroyPool(type);
            }
            _activeSceneRoot = null;
        }

        public T Get<T>() where T : PoolableView
        {
            Type type = typeof(T);

            if (!_pools.TryGetValue(type, out object poolObj))
            {
                Verbose.E($"[GamePool] No active pool for type '{type.Name}'. Call Activate() with this type first.");
                return null;
            }

            Pool<T> pool = (Pool<T>)poolObj;
            return pool.Get();
        }

        public void Return<T>(T instance) where T : PoolableView
        {
            if (!instance) return;
            Type type = typeof(T);
            if (!_pools.TryGetValue(type, out object poolObj))
            {
                UnityEngine.Object.Destroy(instance.gameObject);
                return;
            }
            Pool<T> pool = (Pool<T>)poolObj;
            pool?.Release(instance);
        }

        public void ReturnAll<T>() where T : PoolableView
        {
            Type type = typeof(T);
            if (!_pools.TryGetValue(type, out object poolObj)) return;
            ((Pool<T>)poolObj)?.ReleaseAll();
        }

        public void ReturnAllPools()
        {
            foreach (object poolObj in _pools.Values)
            {
                if (poolObj is IPoolInternal poolInternal) poolInternal.ReleaseAll();
            }
        }

        public void ClearPool<T>() where T : PoolableView
        {
            Type type = typeof(T);
            if (!_pools.ContainsKey(type)) return;
            DestroyPool(type);
        }

        private void CreatePool(Type type, PoolConfiguration config)
        {
            GameObject poolParentGo = new GameObject($"[Pool] {config.PoolName}");
            poolParentGo.transform.SetParent(_activeSceneRoot);
            _poolParents[type] = poolParentGo.transform;
            Type poolType = typeof(Pool<>).MakeGenericType(type);
            object pool = Activator.CreateInstance(poolType, config, poolParentGo.transform);
            if (pool == null)
            {
                Verbose.E($"[GamePool] Failed to create pool for type '{type.Name}'.");
                UnityEngine.Object.Destroy(poolParentGo);
                return;
            }
            _pools[type] = pool;
        }

        private void DestroyPool(Type type)
        {
            if (_pools.TryGetValue(type, out object poolObj))
            {
                if (poolObj is IDisposable disposable) disposable.Dispose();
                _pools.Remove(type);
            }
            if (!_poolParents.TryGetValue(type, out Transform parent) || !parent) return;
            UnityEngine.Object.Destroy(parent.gameObject);
            _poolParents.Remove(type);
        }
    }
}
