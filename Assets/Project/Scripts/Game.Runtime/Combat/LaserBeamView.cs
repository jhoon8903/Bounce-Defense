using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class LaserBeamView : PoolableView
    {
        [SerializeField] private LineRenderer coreLine;
        [SerializeField] private LineRenderer glowLine;
        [SerializeField] private float halfWidth = 4.5f;
        [SerializeField] private float lifetime = 0.7f;
        [SerializeField] private Color coreColor = new Color(0.85f, 0.98f, 1f, 1f);
        [SerializeField] private Color glowColor = new Color(0.30f, 0.85f, 1f, 1f);
        [SerializeField] private float coreWidth = 0.16f;
        [SerializeField] private float glowWidth = 0.80f;
        [SerializeField] private float holdFraction = 0.6f;

        private float _elapsed;
        private bool _alive;

        public bool IsAlive => _alive;

        public void Play(Vector3 worldPos)
        {
            transform.position = worldPos;
            _elapsed = 0f;
            _alive = true;
            Setup(coreLine);
            Setup(glowLine);
            Apply(1f, 1.4f);
        }

        private void Setup(LineRenderer lr)
        {
            if (lr == null) return;
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            lr.SetPosition(1, new Vector3(halfWidth, 0f, 0f));
            lr.enabled = true;
        }

        public bool Tick(float dt)
        {
            if (!_alive || dt <= 0f) return _alive;
            _elapsed += dt;
            float t = lifetime > 0f ? _elapsed / lifetime : 1f;
            if (t >= 1f)
            {
                _alive = false;
                return false;
            }
            float widthMul = t < 0.12f ? Mathf.Lerp(1.4f, 1.0f, t / 0.12f) : 1.0f;
            float alpha = t < holdFraction ? 1f : 1f - (t - holdFraction) / Mathf.Max(0.01f, 1f - holdFraction);
            Apply(alpha, widthMul);
            return true;
        }

        private void Apply(float alpha, float widthMul)
        {
            float flick = 0.88f + 0.12f * Mathf.Abs(Mathf.Sin(_elapsed * 50f));
            SetLine(coreLine, coreColor, alpha * flick, coreWidth * widthMul);
            SetLine(glowLine, glowColor, alpha, glowWidth * widthMul);
        }

        private static void SetLine(LineRenderer lr, Color c, float a, float w)
        {
            if (lr == null) return;
            Color col = c;
            col.a *= Mathf.Clamp01(a);
            lr.startColor = col;
            lr.endColor = col;
            lr.startWidth = w;
            lr.endWidth = w;
        }

        public override void OnInactive()
        {
            base.OnInactive();
            _alive = false;
            if (coreLine != null) coreLine.enabled = false;
            if (glowLine != null) glowLine.enabled = false;
        }
    }
}
