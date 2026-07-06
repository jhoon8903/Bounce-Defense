using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 레이저 행 빔 = 2겹 LineRenderer(넓은 글로우 후광 + 얇고 밝은 코어)로 "레이저"感. 파티클 아님(Daniel 제안).
    //  - CombatVfxController가 _laserPool로 소유. Play(center)서 행 중심에 놓고 ±halfWidth로 뻗음.
    //  - Tick: 스냅-인 플래시(폭 1.4→1.0) → 홀드 → 페이드 + 코어 미세 플리커. 수명은 컨트롤러가 GameDeltaTime으로 전진.
    //  - 색/폭/머터리얼은 프리팹(자식 LineRenderer)에서. 코드가 transform scale 안 건드림(Daniel 규칙).
    [DisallowMultipleComponent]
    public sealed class LaserBeamView : PoolableView
    {
        [SerializeField] private LineRenderer coreLine;  // 얇고 밝은 심
        [SerializeField] private LineRenderer glowLine;  // 넓고 부드러운 후광
        [SerializeField] private float halfWidth = 4.5f; // 좌우 반폭(그리드 9칸 → ±4.5, x=0 중심)
        [SerializeField] private float lifetime = 0.7f;  // 번쩍 후 완전 소멸까지(초)
        [SerializeField] private Color coreColor = new Color(0.85f, 0.98f, 1f, 1f);   // 흰-시안 심
        [SerializeField] private Color glowColor = new Color(0.30f, 0.85f, 1f, 1f);   // 시안 후광
        [SerializeField] private float coreWidth = 0.16f;
        [SerializeField] private float glowWidth = 0.80f;
        [SerializeField] private float holdFraction = 0.6f; // 이 지점(수명비)까지 풀 알파 유지 후 페이드

        private float _elapsed;
        private bool _alive;

        public bool IsAlive => _alive;

        // 스폰 직후 컨트롤러가 호출. 행 중심에 놓고 두 라인을 ±halfWidth로 뻗은 뒤 풀 밝기로 켬.
        public void Play(Vector3 worldPos)
        {
            transform.position = worldPos;
            _elapsed = 0f;
            _alive = true;
            Setup(coreLine);
            Setup(glowLine);
            Apply(1f, 1.4f); // 스폰 순간 = 밝고 넓게(플래시 시작)
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

        // 컨트롤러가 매 틱 GameDeltaTime을 넘겨 호출. false 반환 시 풀 반환.
        public bool Tick(float dt)
        {
            if (!_alive || dt <= 0f) return _alive;
            _elapsed += dt;
            float t = lifetime > 0f ? _elapsed / lifetime : 1f;
            if (t >= 1f) { _alive = false; return false; }
            // 폭: 스냅-인 플래시(1.4→1.0, 첫 12%) 후 유지
            float widthMul = t < 0.12f ? Mathf.Lerp(1.4f, 1.0f, t / 0.12f) : 1.0f;
            // 알파: holdFraction까지 유지 후 선형 페이드
            float alpha = t < holdFraction ? 1f : 1f - (t - holdFraction) / Mathf.Max(0.01f, 1f - holdFraction);
            Apply(alpha, widthMul);
            return true;
        }

        private void Apply(float alpha, float widthMul)
        {
            float flick = 0.88f + 0.12f * Mathf.Abs(Mathf.Sin(_elapsed * 50f)); // 코어 미세 플리커(에너지感)
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
            if (coreLine != null) coreLine.enabled = false; // 풀 재사용 대비 끔
            if (glowLine != null) glowLine.enabled = false;
        }
    }
}
