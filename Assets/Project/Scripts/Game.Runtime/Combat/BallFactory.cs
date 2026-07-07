using System.Collections.Generic;
using Game.Combat;
using Game.Core.Pool;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class BallFactory : IBallFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<BallSourceType, Pool<BallView>> _pools = new();
        private readonly Dictionary<BallView, BallSourceType> _typeOf = new();
        private readonly BallSourceType _fallbackType;
        private int _idCounter;

        public BallFactory(IEnumerable<BallConfig> configs, Transform poolRoot, IObjectResolver resolver)
        {
            _resolver = resolver;
            BallSourceType first = BallSourceType.Normal;
            bool any = false;
            if (configs != null)
            {
                foreach (BallConfig c in configs)
                {
                    if (c == null || c.Prefab == null || _pools.ContainsKey(c.SourceType)) continue;
                    GameObject parentGo = new GameObject($"[Pool] {c.PoolName}");
                    if (poolRoot != null) parentGo.transform.SetParent(poolRoot);
                    _pools[c.SourceType] = new Pool<BallView>(c, parentGo.transform);
                    if (!any)
                    {
                        first = c.SourceType;
                        any = true;
                    }
                }
            }
            _fallbackType = _pools.ContainsKey(BallSourceType.Normal) ? BallSourceType.Normal : first;
        }

        public (BallModel model, BallView view) Create(Vector2 origin, BallSourceType type)
        {
            BallSourceType useType = _pools.ContainsKey(type) ? type : _fallbackType;
            if (!_pools.TryGetValue(useType, out Pool<BallView> pool)) return (null, null);

            BallView view = pool.Get();
            if (view == null) return (null, null);
            _resolver.Inject(view);
            _typeOf[view] = useType;

            BallModel model = new BallModel();
            model.Initialize(origin);
            view.Model = model;
            return (model, view);
        }

        public void Release(BallModel model, BallView view)
        {
            if (view != null)
            {
                view.Model = null;
                BallSourceType type = _typeOf.TryGetValue(view, out BallSourceType t) ? t : _fallbackType;
                _typeOf.Remove(view);
                if (_pools.TryGetValue(type, out Pool<BallView> pool)) pool.Release(view);
            }
            model?.Dispose();
        }

        public string GenerateId() => $"ball_{_idCounter++}";
    }
}
