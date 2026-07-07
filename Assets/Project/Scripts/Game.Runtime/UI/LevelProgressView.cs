using Game.Roguelike;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class LevelProgressView : UiView<LevelModel>
    {
        [SerializeField] private Slider slider;
        [SerializeField] private NumberTextView levelNumber;
        [SerializeField] private CanvasGroup levelUpFlash;
        [SerializeField] private float flashDuration = 1.0f;
        [SerializeField] private float sliderLerpSpeed = 3f;

        private int _lastLevel = 1;
        private float _flashTimer;
        private float _sliderTarget;
        private float _sliderDisplay;

        protected override void RefreshView()
        {
            LevelModel m = Model;
            if (m == null) return;
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                _sliderTarget = m.Progress;
            }
            if (levelNumber != null) levelNumber.SetValue(m.Level);
            if (m.Level > _lastLevel) TriggerFlash();
            _lastLevel = m.Level;
        }

        private void TriggerFlash()
        {
            if (levelUpFlash == null) return;
            _flashTimer = flashDuration;
            levelUpFlash.alpha = 1f;
        }

        private void Update()
        {
            if (slider != null && !Mathf.Approximately(_sliderDisplay, _sliderTarget))
            {
                _sliderDisplay = Mathf.MoveTowards(_sliderDisplay, _sliderTarget, Time.unscaledDeltaTime * sliderLerpSpeed);
                slider.value = _sliderDisplay;
            }
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.unscaledDeltaTime;
                if (levelUpFlash != null) levelUpFlash.alpha = Mathf.Clamp01(_flashTimer / flashDuration);
            }
        }
    }
}
