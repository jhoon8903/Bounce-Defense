using System;
using Game.Core.Base;
using UnityEngine;

namespace Game.Core.Pool
{
    [CreateAssetMenu(fileName = "PoolConfig", menuName = "Game/Configs/PoolConfig")]
    public class PoolConfiguration : ScriptableObject
    {
        [Header("Define")]
        [SerializeField] private string poolName;
        [SerializeField] private GameObject prefab;

        [Header("Setting")]
        [SerializeField] [Min(1)] private int defaultCapacity = 10;
        [SerializeField] [Min(1)] private int maxCapacity = 100;

        public string PoolName => poolName;
        public GameObject Prefab => prefab;
        public int DefaultCapacity => defaultCapacity;
        public int MaxCapacity => maxCapacity;

        public Type GetPoolType()
        {
            if (prefab == null)
            {
                Verbose.E($"[PoolConfiguration] Prefab is null `{poolName}`");
                return null;
            }
            
            if (prefab.TryGetComponent(out PoolableView poolableView)) return poolableView.GetType();
            Verbose.E($"[PoolConfiguration] Prefab '{prefab.name}' does not have PoolableView component");
            return null;
        }
    }
}
