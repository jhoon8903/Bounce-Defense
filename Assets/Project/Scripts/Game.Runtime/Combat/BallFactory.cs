using Game.Combat;
using Game.Core.Pool;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class BallFactory : IBallFactory
    {
        private readonly IPool _pool;
        private readonly IObjectResolver _resolver;
        private int _idCounter;

        public BallFactory(IPool pool, IObjectResolver resolver)
        {
            _pool = pool;
            _resolver = resolver;
        }

        public (BallModel model, BallView view) Create(string id, Vector2 origin, Vector2 direction, float speed, int baseDamage, BallSourceType sourceType)
        {
            BallView view = _pool.Get<BallView>();
            if (view == null) return (null, null);
            _resolver.Inject(view);
            BallModel model = new BallModel();
            model.Initialize(id, origin, direction, speed, baseDamage, sourceType);
            view.Model = model;
            return (model, view);
        }

        public void Release(BallModel model, BallView view)
        {
            if (view != null)
            {
                view.Model = null;
                _pool.Return(view);
            }
            model?.Dispose();
        }

        public string GenerateId() => $"ball_{_idCounter++}";
    }
}
