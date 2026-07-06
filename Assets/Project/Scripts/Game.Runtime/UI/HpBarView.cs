using Game.Runtime.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class HpBarView : UiView<BaseModel>
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private NumberTextView hpNumber; // HP 숫자(카운트다운+팝, 제로 alloc)
        [SerializeField] private float lerpSpeed = 3f;

        private static readonly Color High = new(0.30f, 0.85f, 0.35f);
        private static readonly Color Mid = new(0.95f, 0.80f, 0.20f);
        private static readonly Color Low = new(0.90f, 0.25f, 0.22f);

        private float _target = 1f;
        private float _display = 1f;

        protected override void RefreshView()
        {
            BaseModel m = Model;
            if (m == null) return;
            _target = m.RemainingPercent;
            if (hpNumber != null) hpNumber.SetValue(m.Hp);
        }

        private void Update()
        {
            if (Mathf.Abs(_display - _target) > 0.0001f) _display = Mathf.MoveTowards(_display, _target, lerpSpeed * Time.unscaledDeltaTime);
            if (fillImage == null) return;
            fillImage.fillAmount = _display;
            fillImage.color = _display > 0.5f
                ? Color.Lerp(Mid, High, (_display - 0.5f) * 2f)
                : Color.Lerp(Low, Mid, _display * 2f);
        }
    }
}
