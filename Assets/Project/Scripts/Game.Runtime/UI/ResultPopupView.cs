using Game.Core.Clock;
using Game.Runtime.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.Runtime.UI
{
    // 승/패 결과 팝업(스펙 §5). StageController.StateChanged 구독 → 승/패 패널 페이드+스케일 인 + 시간정지.
    // 재시작 = 씬 리로드(로드아웃·레벨·베이스 전부 초기화 = 실게임 스테이지 재시작). 애니메이션은 unscaled(정지 중 동작).
    // 뷰는 순수 표현: 상태 전이는 StageController가, 여기선 표시/연출/재시작만.
    public sealed class ResultPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;    // 루트 페이드 + 레이캐스트 차단
        [SerializeField] private RectTransform panel;  // 스케일 펀치 대상
        [SerializeField] private Image accentImage;    // 승=초록 / 패=빨강 헤더 스트립
        [SerializeField] private TMP_Text titleText;   // STAGE CLEAR! / DEFEAT
        [SerializeField] private TMP_Text detailText;  // 승리 시 잔여 HP%
        [SerializeField] private Button restartButton;
        [SerializeField] private float animDuration = 0.4f;

        private static readonly Color WinColor = new Color(0.30f, 0.85f, 0.35f);
        private static readonly Color LoseColor = new Color(0.90f, 0.25f, 0.22f);

        private StageController _stage;
        private IClock _clock;
        private bool _shown;
        private float _t;

        [Inject]
        public void Construct(StageController stage, IClock clock)
        {
            _stage = stage;
            _clock = clock;
            _stage.StateChanged += OnStateChanged;
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_stage != null) _stage.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(StageState state)
        {
            if (state == StageState.Won) Show(true);
            else if (state == StageState.Lost) Show(false);
        }

        private void Show(bool win)
        {
            if (_shown) return;
            _shown = true;
            _t = 0f;
            if (_clock != null) _clock.GameSpeed = 0f; // 볼·적 하강 모두 GameDeltaTime 기반 → 정지

            Color c = win ? WinColor : LoseColor;
            if (accentImage != null) accentImage.color = c;
            if (titleText != null) { titleText.color = c; titleText.text = win ? "STAGE CLEAR!" : "DEFEAT"; }
            if (detailText != null)
            {
                detailText.gameObject.SetActive(win);
                if (win) detailText.SetText("남은 체력 {0}%", Mathf.RoundToInt(_stage.Base.RemainingPercent * 100f));
            }
            if (group != null) { group.blocksRaycasts = true; group.interactable = true; }
        }

        private void HideImmediate()
        {
            _shown = false;
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
            if (panel != null) panel.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!_shown || _t >= animDuration) return;
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / animDuration);
            if (group != null) group.alpha = k;
            if (panel != null)
            {
                float s = EaseOutBack(k);
                panel.localScale = new Vector3(s, s, 1f);
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private void Restart()
        {
            if (_clock != null) _clock.GameSpeed = 1f;
            Scene s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex >= 0 ? s.buildIndex : 0);
        }
    }
}
