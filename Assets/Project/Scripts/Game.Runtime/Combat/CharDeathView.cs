using System;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class CharDeathView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] parts;
        [SerializeField] private float duration = 1.15f;
        [SerializeField] private float popUpSpeed = 5.5f;
        [SerializeField] private float spreadSpeed = 3.0f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float spinDeg = 320f;

        private struct Part 
        { 
            public Transform T; 
            public SpriteRenderer Sr; 
            public Vector3 Pos0; 
            public Quaternion Rot0; 
            public Color Col0; 
            public Vector2 Vel; 
            public float AngVel; 
        }
        private Part[] _p;
        private float _t;
        private bool _playing;
        private Action _onDone;

        public bool IsPlaying => _playing;

        public void Play(Action onDone)
        {
            if (_playing || parts == null || parts.Length == 0)
            {
                onDone?.Invoke();
                return;
            }
            _onDone = onDone;
            _t = 0f;
            _p = new Part[parts.Length];
            int n = parts.Length;
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer sr = parts[i];
                Transform t = sr != null ? sr.transform : null;
                float side = n > 1 ? ((float)i / (n - 1)) * 2f - 1f : 0f;
                Vector2 vel = new Vector2(side * spreadSpeed, popUpSpeed);
                _p[i] = new Part
                {
                    T = t, Sr = sr,
                    Pos0 = t != null ? t.localPosition : Vector3.zero,
                    Rot0 = t != null ? t.localRotation : Quaternion.identity,
                    Col0 = sr != null ? sr.color : Color.white,
                    Vel = vel,
                    AngVel = -side * spinDeg + (side == 0f ? spinDeg : 0f),
                };
            }
            _playing = true;
        }

        public void ResetParts()
        {
            _playing = false;
            if (_p == null) return;
            for (int i = 0; i < _p.Length; i++)
            {
                if (_p[i].T == null) continue;
                _p[i].T.localPosition = _p[i].Pos0;
                _p[i].T.localRotation = _p[i].Rot0;
                if (_p[i].Sr != null) _p[i].Sr.color = _p[i].Col0;
            }
        }

        private void Update()
        {
            if (!_playing) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            float k = Mathf.Clamp01(_t / duration);
            for (int i = 0; i < _p.Length; i++)
            {
                Part p = _p[i];
                if (p.T == null) continue;
                p.Vel.y += gravity * dt;
                p.T.localPosition += (Vector3)(p.Vel * dt);
                p.T.localRotation *= Quaternion.Euler(0f, 0f, p.AngVel * dt);
                if (p.Sr != null)
                {
                    Color c = p.Col0;
                    c.a = 1f - k;
                    p.Sr.color = c;
                }
                _p[i] = p;
            }
            if (!(_t >= duration)) return;
            _playing = false;
            Action done = _onDone; _onDone = null;
            done?.Invoke();
        }
    }
}
