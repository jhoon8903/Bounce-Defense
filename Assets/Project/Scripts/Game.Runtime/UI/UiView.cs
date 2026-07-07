using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.UI
{
    public abstract class UiView<TModel> : MonoBehaviour, IObserver where TModel : class, IObservable
    {
        private TModel _model;
        public TModel Model => _model;
        public bool IsBound => _model != null;

        public void Bind(TModel model)
        {
            if (_model == model) return;
            _model?.RemoveObserver(this);
            _model = model;
            if (_model == null) return;
            _model.AddObserver(this);
            RefreshView();
        }

        public void Unbind()
        {
            if (_model == null) return;
            _model.RemoveObserver(this);
            _model = null;
        }

        protected virtual void OnEnable()
        {
            if (_model == null) return;
            _model.AddObserver(this);
            RefreshView();
        }

        protected virtual void OnDisable() => _model?.RemoveObserver(this);

        public void OnChanged(IObservable observable)
        {
            if (observable != _model) return;
            RefreshView();
        }

        protected abstract void RefreshView();
    }
}
