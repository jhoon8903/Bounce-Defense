using Game.Roguelike;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class LevelProgressView : UiView<LevelModel>
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private CanvasGroup levelUpFlash;
        [SerializeField] private float flashDuration = 1.0f;

        private int _lastLevel = 1;
        private float _flashTimer;

        protected override void RefreshView()
        {
            LevelModel m = Model;
            if (m == null) return;
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = m.Progress;
            }
            if (levelText != null) levelText.SetText(m.Level.ToString());
            if (m.Level > _lastLevel) TriggerFlash();      // 레벨업 → 플래시
            _lastLevel = m.Level;                          // 리셋(레벨 하강)도 여기서 동기화
        }

        private void TriggerFlash()
        {
            if (levelUpFlash == null) return;
            _flashTimer = flashDuration;
            levelUpFlash.alpha = 1f;
        }
        
        private void Update()
        {
            if (_flashTimer <= 0f) return;
            _flashTimer -= Time.unscaledDeltaTime;
            if (levelUpFlash != null) levelUpFlash.alpha = Mathf.Clamp01(_flashTimer / flashDuration);
        }
    }
}
