using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.UI
{
    // 비풀링 씬 상주 UI 뷰 베이스. BaseView<T>는 PoolableView(풀 대상)라 화면 UI엔 부적합 →
    // Observable 모델 구독 + RefreshView만 갖는 경량 옵저버. (Daniel의 Observer 패턴 준수)
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

        // 비활성→활성 복귀 시 재구독(AddObserver는 Contains 가드라 이중등록 안전).
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
