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

        public (BallModel model, BallView view) Create(Vector2 origin)
        {
            BallView view = _pool.Get<BallView>();
            if (view == null) return (null, null);
            _resolver.Inject(view);
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
                _pool.Return(view);
            }
            model?.Dispose();
        }

        public string GenerateId() => $"ball_{_idCounter++}";
    }
}
