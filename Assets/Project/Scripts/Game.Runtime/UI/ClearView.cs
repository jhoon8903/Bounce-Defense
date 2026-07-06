using Game.Runtime.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Runtime.UI
{
    // 성공 팝업 연출(스펙 #2, UI는 Daniel). Shine 회전 + Head/Staff 흔들 + Star1/2/3 (0,0)→제자리 슬라이드(스태거) + 잔여HP 표기.
    // Restart = 스테이지 재시작(씬 리로드). 애니메이션은 unscaled(GameSpeed 0 정지 중 동작).
    public sealed class ClearView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform shine;
        [SerializeField] private RectTransform head;
        [SerializeField] private RectTransform staff;
        [SerializeField] private RectTransform star1;
        [SerializeField] private RectTransform star2;
        [SerializeField] private RectTransform star3;
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private EventBtn restartButton;
        [SerializeField] private DTResultView dtResult; // 볼별 데미지 목록(자식). 미배선 시 GetComponentInChildren 폴백.
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float shineSpeedDeg = 40f;
        [SerializeField] private float wobbleAmpDeg = 9f;
        [SerializeField] private float wobbleFreq = 7f;
        [SerializeField] private float starDuration = 0.35f;
        [SerializeField] private float starStagger = 0.14f;

        private bool _shown;
        private float _t;
        private Quaternion _headBaseRot, _staffBaseRot;
        private RectTransform[] _stars;
        private Vector2[] _starTargets;

        private void Awake()
        {
            if (head != null) _headBaseRot = head.localRotation;
            if (staff != null) _staffBaseRot = staff.localRotation;
            _stars = new[] { star1, star2, star3 };
            _starTargets = new Vector2[_stars.Length];
            for (int i = 0; i < _stars.Length; i++) if (_stars[i] != null) _starTargets[i] = _stars[i].anchoredPosition;
            if (restartButton != null) restartButton.Clicked += Restart;
            if (dtResult == null) dtResult = GetComponentInChildren<DTResultView>(true);
            HideImmediate();
        }

        public void Show(float remainingPercent, DamageStats stats)
        {
            if (_shown) return;
            _shown = true; _t = 0f;
            if (group != null) { group.blocksRaycasts = true; group.interactable = true; }
            for (int i = 0; i < _stars.Length; i++) if (_stars[i] != null) _stars[i].anchoredPosition = Vector2.zero; // 시작점=중앙
            if (remainingText != null) remainingText.SetText("{0}%", Mathf.RoundToInt(Mathf.Clamp01(remainingPercent) * 100f));
            if (dtResult != null) dtResult.Populate(stats); // 자식 DTResult에 스킬별 데미지 채움
            gameObject.SetActive(true);
        }

        public void HideImmediate()
        {
            _shown = false;
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
        }

        private void Update()
        {
            if (!_shown) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (group != null && group.alpha < 1f) group.alpha = Mathf.Clamp01(group.alpha + dt / Mathf.Max(0.01f, fadeDuration));
            if (shine != null) shine.Rotate(0f, 0f, -shineSpeedDeg * dt);
            if (head != null) head.localRotation = _headBaseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * wobbleFreq) * wobbleAmpDeg);
            if (staff != null) staff.localRotation = _staffBaseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * wobbleFreq * 0.9f + 0.6f) * wobbleAmpDeg);
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] == null) continue;
                float k = starDuration > 0f ? Mathf.Clamp01((_t - i * starStagger) / starDuration) : 1f;
                _stars[i].anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, _starTargets[i], EaseOutBack(k));
            }
        }

        private static float EaseOutBack(float x)
        {
            if (x <= 0f) return 0f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private void Restart()
        {
            Scene s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex >= 0 ? s.buildIndex : 0);
        }
    }
}
