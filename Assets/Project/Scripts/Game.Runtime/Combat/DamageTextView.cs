using Game.Core.Pool;
using TMPro;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageTextView : PoolableView
    {
        [SerializeField] private TMP_Text label;

        private const float Lifetime   = 0.40f;
        private const float RiseSpeed  = 2f;
        private const float FadeStart  = 0.4f;
        private const float PopIn      = 0.20f;
        private const float PopSettle  = 0.2f;
        private const float Overshoot  = 1.50f;
        private const float StartScale = 0.50f;
        private const float EndScale   = 0.82f;
        private const float CritScale  = 1.7f;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color CritColor = new(1f, 0.28f, 0.22f, 1f);

        private Vector3 _baseScale = Vector3.one;
        private bool    _baseCaptured;
        private float   _elapsed;
        private Vector3 _velocity;
        private float   _critMul = 1f;
        private bool    _alive;

        public bool IsAlive => _alive;

        public void Play(int amount, bool isCrit, Vector3 worldPos)
        {
            if (!_baseCaptured)
            {
                _baseScale = transform.localScale;
                _baseCaptured = true;
            }
            transform.position = worldPos;
            if (label != null)
            {
                label.SetText("{0}", amount);
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
            _velocity.x *= Mathf.Clamp01(1f - 6f * dt);
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
