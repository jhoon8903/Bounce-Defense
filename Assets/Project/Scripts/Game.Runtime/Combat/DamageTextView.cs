using Game.Core.Pool;
using TMPro;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 풀링되는 월드공간 데미지 숫자(Daniel의 DamageCanvas 프리팹 = 월드스페이스 Canvas + 자식 TextMeshProUGUI).
    // 값/색은 스폰 시 고정(fire-and-forget). 애니메이션 상태만 보유 — 틱은 HitFeedbackController가
    // IClock.GameDeltaTime으로 구동(§11-9 자체 Update 금지). label은 TMP_Text(UGUI/3D 공통 부모).
    [DisallowMultipleComponent]
    public sealed class DamageTextView : PoolableView
    {
        [SerializeField] private TMP_Text label;

        // ---- 애니메이션 상수(월드 단위, cellSize 1.0 기준) ----
        private const float Lifetime   = 0.40f;
        private const float RiseSpeed  = 2f;  // 상승 속도(units/s)
        private const float FadeStart  = 0.4f;  // 이 시점부터 알파 감쇠
        private const float PopIn      = 0.20f;  // 0→오버슈트
        private const float PopSettle  = 0.2f;  // 오버슈트→기본
        private const float Overshoot  = 1.50f;
        private const float StartScale = 0.50f;
        private const float EndScale   = 0.82f;  // 마지막 축소
        private const float CritScale  = 2f;  // 크리 크기 배수

        private static readonly Color NormalColor = Color.white;
        private static readonly Color CritColor = new(1f, 0.28f, 0.22f, 1f);

        private Vector3 _baseScale = Vector3.one; // 프리팹 로컬스케일(팝 배수의 기준)
        private bool    _baseCaptured;
        private float   _elapsed;
        private Vector3 _velocity; // 상승 + 수평 드리프트
        private float   _critMul = 1f;
        private bool    _alive;

        public bool IsAlive => _alive;

        // 스폰 직후 컨트롤러가 호출. 값/색/위치/속도 초기화(재사용 인스턴스 전량 덮어씀).
        public void Play(int amount, bool isCrit, Vector3 worldPos)
        {
            if (!_baseCaptured) { _baseScale = transform.localScale; _baseCaptured = true; }
            transform.position = worldPos;
            if (label != null)
            {
                label.SetText("{0}", amount); // 제로할당(.text/.ToString() 금지)
                label.color = isCrit ? CritColor : NormalColor;
                label.alpha = 1f;
            }
            _critMul = isCrit ? CritScale : 1f;
            _elapsed = 0f;
            float drift = Random.Range(-0.45f, 0.45f);
            _velocity = new Vector3(drift, RiseSpeed, 0f);
            transform.localScale = _baseScale * (StartScale * _critMul);
            _alive = true;
        }

        // 컨트롤러가 매 틱 GameDeltaTime을 넘겨 호출. false 반환 시 컨트롤러가 풀 반환.
        public bool Tick(float dt)
        {
            if (!_alive || dt <= 0f) return _alive;
            _elapsed += dt;
            if (_elapsed >= Lifetime)
            {
                _alive = false; 
                return false;
            }
            transform.position += _velocity * dt;
            _velocity.x *= Mathf.Clamp01(1f - 6f * dt); // 수평 드리프트 감쇠
            transform.localScale = _baseScale * (ScaleMul() * _critMul);
            if (label != null) label.alpha = Alpha();
            return true;
        }

        private float ScaleMul()
        {
            if (_elapsed < PopIn)     return Mathf.Lerp(StartScale, Overshoot, _elapsed / PopIn);
            if (_elapsed < PopSettle) return Mathf.Lerp(Overshoot, 1f, (_elapsed - PopIn) / (PopSettle - PopIn));
            if (_elapsed < FadeStart) return 1f;
            return Mathf.Lerp(1f, EndScale, (_elapsed - FadeStart) / (Lifetime - FadeStart));
        }

        private float Alpha()
        {
            if (_elapsed < FadeStart) return 1f;
            return 1f - (_elapsed - FadeStart) / (Lifetime - FadeStart);
        }

        // 풀 반환 시 무조건 클린 리셋 — 재사용 인스턴스가 이전 값/색/알파/스케일을 보이지 않게.
        public override void OnInactive()
        {
            base.OnInactive();
            _alive = false;
            if (label != null)
            {
                label.SetText(string.Empty); 
                label.alpha = 1f; 
                label.color = NormalColor;
            }
            if (_baseCaptured) transform.localScale = _baseScale;
        }
    }
}
