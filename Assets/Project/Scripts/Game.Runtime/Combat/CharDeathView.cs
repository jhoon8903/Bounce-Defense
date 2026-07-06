using System;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 캐릭터 사망 분리 연출(스펙 #4). HP 0 시 지팡이/몸통/머리 스프라이트가 각자 튕겨나가 회전·낙하·페이드.
    // 게임 정지(GameSpeed=0) 중에 재생돼야 하므로 unscaled 시간. 완료 시 onDone → 실패 팝업 오픈 게이트.
    public sealed class CharDeathView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] parts;   // Body, HeadView, StaffView
        [SerializeField] private float duration = 1.15f;
        [SerializeField] private float popUpSpeed = 5.5f;   // 위로 튕기는 초기 속도
        [SerializeField] private float spreadSpeed = 3.0f;  // 좌우로 벌어지는 속도
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float spinDeg = 320f;      // 초당 회전(부호는 좌우로)

        private struct Part { public Transform t; public SpriteRenderer sr; public Vector3 pos0; public Quaternion rot0; public Color col0; public Vector2 vel; public float angVel; }
        private Part[] _p;
        private float _t;
        private bool _playing;
        private Action _onDone;

        public bool IsPlaying => _playing;

        public void Play(Action onDone)
        {
            if (_playing || parts == null || parts.Length == 0) { onDone?.Invoke(); return; }
            _onDone = onDone;
            _t = 0f;
            _p = new Part[parts.Length];
            int n = parts.Length;
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer sr = parts[i];
                Transform t = sr != null ? sr.transform : null;
                // 파트별 초기 속도: 위로 팝 + 인덱스 기준 좌우 부채꼴(중앙0, 좌우로 벌어짐)
                float side = n > 1 ? ((float)i / (n - 1)) * 2f - 1f : 0f; // -1..+1
                Vector2 vel = new Vector2(side * spreadSpeed, popUpSpeed);
                _p[i] = new Part
                {
                    t = t, sr = sr,
                    pos0 = t != null ? t.localPosition : Vector3.zero,
                    rot0 = t != null ? t.localRotation : Quaternion.identity,
                    col0 = sr != null ? sr.color : Color.white,
                    vel = vel,
                    angVel = -side * spinDeg + (side == 0f ? spinDeg : 0f),
                };
            }
            _playing = true;
        }

        // 재시작(씬 리로드 안 쓰는 경로) 대비 원위치 복구.
        public void ResetParts()
        {
            _playing = false;
            if (_p == null) return;
            for (int i = 0; i < _p.Length; i++)
            {
                if (_p[i].t == null) continue;
                _p[i].t.localPosition = _p[i].pos0;
                _p[i].t.localRotation = _p[i].rot0;
                if (_p[i].sr != null) _p[i].sr.color = _p[i].col0;
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
                if (p.t == null) continue;
                p.vel.y += gravity * dt;
                p.t.localPosition += (Vector3)(p.vel * dt);
                p.t.localRotation *= Quaternion.Euler(0f, 0f, p.angVel * dt);
                if (p.sr != null) { Color c = p.col0; c.a = 1f - k; p.sr.color = c; }
                _p[i] = p;
            }
            if (_t >= duration)
            {
                _playing = false;
                Action done = _onDone; _onDone = null;
                done?.Invoke();
            }
        }
    }
}
