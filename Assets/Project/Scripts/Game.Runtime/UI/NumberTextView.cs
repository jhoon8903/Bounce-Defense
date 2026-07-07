using TMPro;
using UnityEngine;

namespace Game.Runtime.UI
{
    public sealed class NumberTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float countDuration = 0.3f;
        [SerializeField] private float popScale = 1.25f;
        [SerializeField] private float popDuration = 0.16f;

        private string _format = "{0}";
        private int _from, _to, _shown = int.MinValue;
        private float _countT, _popT = 999f;
        private RectTransform _rt;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (text != null)
            {
                _rt = text.rectTransform;
                _baseScale = _rt.localScale;
            }
        }

        public void SetFormat(string format)
        {
            string f = string.IsNullOrEmpty(format) ? "{0}" : format;
            if (_format == f) return;
            _format = f;
            if (_shown != int.MinValue) Render(_shown);
        }

        public void SetValue(int value)
        {
            if (_shown == int.MinValue)
            {
                _from = _to = _shown = value;
                Render(value);
                return;
            }
            if (value == _to) return;
            _from = _shown;
            _to = value;
            _countT = 0f;
            _popT = 0f;
        }

        private void Update()
        {
            if (_shown != _to)
            {
                _countT += Time.unscaledDeltaTime;
                float k = countDuration > 0f ? Mathf.Clamp01(_countT / countDuration) : 1f;
                int v = Mathf.RoundToInt(Mathf.Lerp(_from, _to, k));
                if (v != _shown)
                {
                    _shown = v;
                    Render(v);
                }
            }
            if (_rt != null && _popT < popDuration)
            {
                _popT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_popT / popDuration);
                float s = 1f + (popScale - 1f) * Mathf.Sin(k * Mathf.PI);
                _rt.localScale = _baseScale * s;
            }
        }

        private void Render(int v) { if (text != null) text.SetText(_format, v); }
    }
}
