using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Runtime.UI
{
    public sealed class DefeatedView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform head;
        [SerializeField] private EventBtn reviveButton;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float wobbleAmpDeg = 12f;
        [SerializeField] private float wobbleFreq = 9f;
        [SerializeField] private float rattleAmp = 3f;

        private bool _shown;
        private float _t;
        private Vector2 _headBasePos;
        private Quaternion _headBaseRot;

        private void Awake()
        {
            if (head != null)
            {
                _headBasePos = head.anchoredPosition;
                _headBaseRot = head.localRotation;
            }
            if (reviveButton != null) reviveButton.Clicked += Restart;
            HideImmediate();
        }

        public void Show()
        {
            if (_shown) return;
            _shown = true; _t = 0f;
            if (group != null)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            gameObject.SetActive(true);
        }

        public void HideImmediate()
        {
            _shown = false;
            if (group == null) return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void Update()
        {
            if (!_shown) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (group != null && group.alpha < 1f) group.alpha = Mathf.Clamp01(group.alpha + dt / Mathf.Max(0.01f, fadeDuration));
            if (head == null) return;
            head.localRotation = _headBaseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * wobbleFreq) * wobbleAmpDeg);
            head.anchoredPosition = _headBasePos + new Vector2(
                Mathf.Sin(_t * wobbleFreq * 2.3f) * rattleAmp,
                Mathf.Cos(_t * wobbleFreq * 1.7f) * rattleAmp * 0.6f);
        }

        private void Restart()
        {
            Scene s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex >= 0 ? s.buildIndex : 0);
        }
    }
}
