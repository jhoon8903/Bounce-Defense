using System.Collections.Generic;
using Game.Combat;
using Game.Core.Pool;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    // 볼 타입별 풀 소유(§11-9 결정: BallFactory가 타입별 Pool<BallView> 소유).
    //  - 각 BallConfig(=타입별 프리팹) 하나당 Pool<BallView> 하나(타입으로 키). 스폰 시 spec 타입의 풀에서 그 프리팹을 꺼낸다.
    //  - GamePool은 View 타입으로 풀을 나눠 6개 BallView 프리팹이 한 풀로 충돌 → 여기서 config별로 분리(Game.Core 풀 무수정).
    //  - Pool<T>(Game.Core 템플릿)를 그대로 재사용 — config.Prefab을 인스턴스화/재활용.
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
                    if (!any) { first = c.SourceType; any = true; }
                }
            }
            // 폴백: Normal 풀 있으면 Normal, 없으면 처음 등록된 타입(스킬볼 config 미배선 시 코어 루프 보존).
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
