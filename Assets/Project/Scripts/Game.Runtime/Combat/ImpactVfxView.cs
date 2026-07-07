using System.Collections.Generic;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class ImpactVfxView : PoolableView
    {
        [SerializeField] private float lifetime = 0.7f;

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

        public bool Tick(float dt)
        {
            if (!_alive || dt <= 0f) return _alive;
            _elapsed += dt;
            if (!(_elapsed >= lifetime)) return true;
            _alive = false;
            return false;
        }

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
