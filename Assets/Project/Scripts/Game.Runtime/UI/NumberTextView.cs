using TMPro;
using UnityEngine;

namespace Game.Runtime.UI
{
    // 숫자 텍스트 애니메이터(공용 헬퍼). 값 변경 시 카운트업(보간) + 팝(스케일 펀치).
    // TMP SetText만 사용(제로 alloc). unscaled 시간 → 드래프트/결과 정지(GameSpeed 0) 중에도 동작.
    // format 예: "{0}" 또는 "{0} / 300"(리터럴 포함 가능 — TMP SetText 지원 확인).
    public sealed class NumberTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float countDuration = 0.3f; // 카운트업 시간
        [SerializeField] private float popScale = 1.25f;     // 변화 시 팝 최대 배율
        [SerializeField] private float popDuration = 0.16f;

        private string _format = "{0}";
        private int _from, _to, _shown = int.MinValue;
        private float _countT, _popT = 999f;
        private RectTransform _rt;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (text != null) { _rt = text.rectTransform; _baseScale = _rt.localScale; }
        }

        // 포맷 지정(예: "{0} / " + maxHp). 현재 값 유지한 채 다시 렌더.
        public void SetFormat(string format)
        {
            string f = string.IsNullOrEmpty(format) ? "{0}" : format;
            if (_format == f) return;
            _format = f;
            if (_shown != int.MinValue) Render(_shown);
        }

        // 값 변경 → 카운트업 + 팝. 첫 세팅은 즉시(애니메이션 없음).
        public void SetValue(int value)
        {
            if (_shown == int.MinValue) { _from = _to = _shown = value; Render(value); return; }
            if (value == _to) return;
            _from = _shown;
            _to = value;
            _countT = 0f;
            _popT = 0f; // 팝 트리거
        }

        private void Update()
        {
            if (_shown != _to)
            {
                _countT += Time.unscaledDeltaTime;
                float k = countDuration > 0f ? Mathf.Clamp01(_countT / countDuration) : 1f;
                int v = Mathf.RoundToInt(Mathf.Lerp(_from, _to, k));
                if (v != _shown) { _shown = v; Render(v); }
            }
            if (_rt != null && _popT < popDuration)
            {
                _popT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_popT / popDuration);
                float s = 1f + (popScale - 1f) * Mathf.Sin(k * Mathf.PI); // 0→peak→0
                _rt.localScale = _baseScale * s;
            }
        }

        private void Render(int v) { if (text != null) text.SetText(_format, v); }
    }
}
