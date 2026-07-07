using Game.Core.Pool;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyFactory : IEnemyFactory
    {
        private readonly IPool _pool;
        private readonly IObjectResolver _resolver;
        private int _idCounter;

        public EnemyFactory(IPool pool, IObjectResolver resolver)
        {
            _pool = pool;
            _resolver = resolver;
        }

        public (EnemyModel model, EnemyView view) Create(string id, EnemyDefinition definition, Vector2 position, float hpScale = 1f)
        {
            EnemyView view = _pool.Get<EnemyView>();
            if (view == null) return (null, null);
            _resolver.Inject(view);
            EnemyModel model = new EnemyModel();
            model.Initialize(id, definition, position, hpScale);
            view.Model = model;
            return (model, view);
        }

        public void Release(EnemyModel model, EnemyView view)
        {
            if (view != null)
            {
                view.Model = null;
                _pool.Return(view);
            }
            model?.Dispose();
        }

        public string GenerateId() => $"enemy_{_idCounter++}";
    }
}
