using System.Collections.Generic;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class ImpactVfxView : PoolableView
    {
        [SerializeField] private float lifetime = 0.7f; // 풀 반환까지 수명(초) — 파티클 지속보다 약간 길게.

        private ParticleSystem[] _roots;
        private float _elapsed;
        private bool _alive;

        public bool IsAlive => _alive;

        private void Awake() => CacheRoots();

        private void CacheRoots()
        {
            ParticleSystem[] all = GetComponentsInChildren<ParticleSystem>(true);
            List<ParticleSystem> roots = new(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                if (!HasParticleAncestor(all[i])) roots.Add(all[i]);
            }
            _roots = roots.ToArray();
        }

        private static bool HasParticleAncestor(ParticleSystem ps)
        {
            Transform t = ps.transform.parent;
            while (t != null)
            {
                if (t.GetComponent<ParticleSystem>() != null) return true;
                t = t.parent;
            }
            return false;
        }

        // 스폰 직후 컨트롤러가 호출. 위치만 세팅 + 최상위 파티클 전부 리셋 후 재생.
        // 크기는 프리팹 자체(transform scale + PS 내부)에서만 정함 — 코드가 transform scale을 건드리지 않는다(Daniel 규칙: 스케일은 왜곡, 내부로).
        public void Play(Vector3 worldPos)
        {
            transform.position = worldPos;
            _elapsed = 0f;
            _alive = true;
            if (_roots == null) CacheRoots();
            for (int i = 0; i < _roots.Length; i++)
            {
                ParticleSystem ps = _roots[i];
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        // 컨트롤러가 매 틱 GameDeltaTime을 넘겨 호출. false 반환 시 컨트롤러가 풀 반환.
        public bool Tick(float dt)
        {
            if (!_alive || dt <= 0f) return _alive;
            _elapsed += dt;
            if (!(_elapsed >= lifetime)) return true;
            _alive = false;
            return false;
        }

        // 풀 반환 시 클린 리셋 — 재사용 인스턴스가 이전 파티클을 이어 그리지 않게.
        public override void OnInactive()
        {
            base.OnInactive();
            _alive = false;
            if (_roots == null) return;
            for (int i = 0; i < _roots.Length; i++)
            {
                if (_roots[i] != null) _roots[i].Clear(true);
            }
        }
    }
}
