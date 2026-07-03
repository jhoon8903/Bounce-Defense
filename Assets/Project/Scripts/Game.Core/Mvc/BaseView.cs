using Game.Core.Observer;
using Game.Core.Pool;

namespace Game.Core.Mvc
{
    public abstract class BaseView<TModel> : PoolableView, IObserver where TModel : class, IObservable
    {
        private TModel _model;
        public TModel Model
        {
            get => _model;
            set
            {
                if (_model == value) return;
                UnbindModel();
                _model = value;
                BindModel();
            }
        }

        public bool IsModelBound => _model != null;

        protected virtual void OnEnable()
        {
            if (_model == null) return;
            _model.AddObserver(this);
            RefreshView();
        }

        protected virtual void OnDisable() => _model?.RemoveObserver(this);

        protected override void OnDestroy()
        {
            UnbindModel();
            base.OnDestroy();
        }

        private void BindModel()
        {
            if (_model == null) return;
            _model.AddObserver(this);
            OnModelBound(_model);
            RefreshView();
        }

        private void UnbindModel()
        {
            if (_model == null) return;
            _model.RemoveObserver(this);
            OnModelUnbound(_model);
        }

        public void OnChanged(IObservable observable)
        {
            if (observable != _model) return;
            OnModelChanged(_model);
            RefreshView();
        }

        public override void OnInactive()
        {
            UnbindModel();
            _model = null;
            base.OnInactive();
        }

        protected abstract void OnModelBound(TModel model);
        protected abstract void OnModelUnbound(TModel model);
        protected abstract void OnModelChanged(TModel model);
        protected abstract void RefreshView();
    }
}
