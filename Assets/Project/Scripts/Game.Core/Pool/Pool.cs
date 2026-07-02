using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Core.Base;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Game.Core.Pool
{
    public sealed class Pool<T> : IPoolInternal, IDisposable where T : PoolableView
    {
        private readonly PoolConfiguration _config;
        private readonly Transform _parent;
        private readonly IObjectPool<T> _pool;
        private readonly HashSet<T> _activeInstances = new();

        public IReadOnlyCollection<T> ActiveInstances => _activeInstances;
        public int ActiveCount => _activeInstances.Count;
        public int TotalCount => _pool.CountInactive + _activeInstances.Count;

        public Pool(PoolConfiguration config, Transform parent)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _pool = new ObjectPool<T>(
                createFunc: CreateInstance,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy,
                collectionCheck: _config.CollectionCheck,
                defaultCapacity: _config.DefaultCapacity,
                maxSize: _config.MaxCapacity
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            T instance = _pool.Get();
            _activeInstances.Add(instance);
            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(T instance)
        {
            if (!instance) return;
            _activeInstances.Remove(instance);
            _pool.Release(instance);
        }

        public void ReleaseAll()
        {
            T[] instances = new T[_activeInstances.Count];
            _activeInstances.CopyTo(instances);
            for (int i = 0; i < instances.Length; i++)
            {
                T instance = instances[i];
                if (instance && instance.gameObject.activeInHierarchy) Release(instance);
            }
        }

        public void Clear()
        {
            _activeInstances.Clear();
            _pool.Clear();
        }

        public void Dispose() => Clear();

        private T CreateInstance()
        {
            if (!_config.Prefab)
            {
                Verbose.E($"[Pool] Prefab is null in PoolConfig: {_config.PoolName}");
                return null;
            }

            GameObject instance = Object.Instantiate(_config.Prefab, _parent);
            instance.name = _config.PoolName;
            if (instance.TryGetComponent(out T component)) return component;
            Verbose.E($"[Pool] Prefab '{_config.Prefab.name}' does not have component of type {typeof(T).Name}");
            Object.Destroy(instance);
            return null;
        }

        private void OnGet(T instance)
        {
            if (!instance) return;
            instance.gameObject.SetActive(true);
            instance.OnActive();
        }

        private void OnRelease(T instance)
        {
            if (!instance) return;
            instance.OnInactive();
            instance.transform.SetParent(_parent);
            instance.gameObject.SetActive(false);
        }

        private void OnDestroy(T instance)
        {
            if (!instance) return;
            _activeInstances.Remove(instance);
            Object.Destroy(instance.gameObject);
        }
    }
}
