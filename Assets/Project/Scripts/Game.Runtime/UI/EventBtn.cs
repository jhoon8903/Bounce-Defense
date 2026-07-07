using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Runtime.UI
{
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
            if (now < _globalLockUntil || now < _selfLockUntil) return;
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

        protected virtual void OnClicked() { }

        public void ClearClicked() => Clicked = null;
    }
}
