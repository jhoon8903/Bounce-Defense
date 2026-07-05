using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Runtime.UI
{
    // Daniel EventBtn 패턴 경량 포팅(UniTask/DOTween/JSAM 의존 없음). Unity Button 대신 IPointerClickHandler.
    //  - 전역 락(static): 한 번 눌리면 모든 EventBtn이 ThrottleTime 동안 잠김 → 카드 동시/연타 선택 차단.
    //  - per-button + 전역 throttle: Time.unscaledTime 기준 → 드래프트 일시정지(IClock.GameSpeed=0) 중에도 정확.
    //  - _isProcessing: 콜백 실행 중 재진입 차단.
    // 시각/사운드 피드백이 필요하면 OnClicked를 override(파생) — 여기선 순수 입력 게이팅만.
    public class EventBtn : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] protected bool interactable = true;
        [SerializeField] protected float throttleTime = 0.5f;

        private static float _globalLockUntil;
        private float _selfLockUntil;
        private bool _isProcessing;

        public event Action Clicked;
        public bool Interactable { get => interactable; set => interactable = value; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || _isProcessing) return;
            float now = Time.unscaledTime;
            if (now < _globalLockUntil || now < _selfLockUntil) return; // 전역/자기 throttle
            _globalLockUntil = now + throttleTime;
            _selfLockUntil = now + throttleTime;

            _isProcessing = true;
            try
            {
                OnClicked();
                Clicked?.Invoke();
            }
            finally { _isProcessing = false; }
        }

        protected virtual void OnClicked() { } // 파생 훅(연출/사운드)

        public void ClearClicked() => Clicked = null;
    }
}
