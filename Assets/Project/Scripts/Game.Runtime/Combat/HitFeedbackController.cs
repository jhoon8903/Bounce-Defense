using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Core.Pool;
using Game.Events;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class HitFeedbackController : BaseController
    {
        private const float FlashDuration = 0.2f;
        private const float RecoilDuration = 0.3f;
        private const float RecoilDistance = 0.4f;

        private readonly IPool _pool;
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;

        private readonly List<DamageTextView> _activeTexts = new();
        private readonly Dictionary<EnemyView, HitFxState> _hitFx = new();
        private readonly List<EnemyView> _fxKeyCache = new();

        private struct HitFxState
        {
            public float FlashElapsed;
            public float RecoilElapsed;
            public Vector2 RecoilDir;
            public EnemyModel Model;
        }

        public HitFeedbackController(IPool pool, IClock clock, CombatEventHub hub)
        {
            _pool = pool;
            _clock = clock;
            _hub = hub;
        }

        protected override void OnInitialize()
        {
            _hub.OnHit += HandleHit;
            _clock.OnTick += HandleTick;
        }

        protected override void OnDispose()
        {
            _hub.OnHit -= HandleHit;
            _clock.OnTick -= HandleTick;
            for (int i = 0; i < _activeTexts.Count; i++)
            {
                if (_activeTexts[i] != null) _pool.Return(_activeTexts[i]);
            }
            _activeTexts.Clear();
            _hitFx.Clear();
        }

        protected override void OnReset() { }
        protected override void OnTick(float _) { }
        protected override void OnFixedTick(float _) { }

        private void HandleHit(EnemyView view, Vector2 worldPos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType, DamageKind kind)
        {
            if (amount > 0)
            {
                DamageTextView text = _pool.Get<DamageTextView>();
                if (text != null)
                {
                    text.Play(amount, isCrit, new Vector3(worldPos.x, worldPos.y, 0f));
                    _activeTexts.Add(text);
                }
            }
            if (view == null || !view.IsModelBound) return;
            HitFxState st = _hitFx.GetValueOrDefault(view);
            st.Model = view.Model;
            st.FlashElapsed = 0f;
            if (hitDir.sqrMagnitude > 1e-6f)
            {
                st.RecoilElapsed = 0f;
                st.RecoilDir = hitDir.normalized;
            }
            else if (st.RecoilDir.sqrMagnitude < 1e-6f) st.RecoilElapsed = RecoilDuration;
            _hitFx[view] = st;
        }

        private void HandleTick()
        {
            float dt = _clock.GameDeltaTime;
            if (dt <= 0f) return;

            for (int i = _activeTexts.Count - 1; i >= 0; i--)
            {
                DamageTextView t = _activeTexts[i];
                if (t != null && t.Tick(dt)) continue;
                if (t != null) _pool.Return(t);
                _activeTexts.RemoveAt(i);
            }

            if (_hitFx.Count == 0) return;
            _fxKeyCache.Clear();
            _fxKeyCache.AddRange(_hitFx.Keys);
            for (int k = _fxKeyCache.Count - 1; k >= 0; k--)
            {
                EnemyView view = _fxKeyCache[k];
                if (!_hitFx.TryGetValue(view, out HitFxState st)) continue;
                if (view == null || !view.IsModelBound || !ReferenceEquals(view.Model, st.Model))
                {
                    _hitFx.Remove(view);
                    continue;
                }

                if (st.FlashElapsed < FlashDuration)
                {
                    st.FlashElapsed += dt;
                    view.SetHitFlash(st.FlashElapsed >= FlashDuration ? 0f : 1f - st.FlashElapsed / FlashDuration);
                }
                if (st.RecoilElapsed < RecoilDuration)
                {
                    st.RecoilElapsed += dt;
                    float k01 = st.RecoilElapsed >= RecoilDuration ? 0f : 1f - st.RecoilElapsed / RecoilDuration;
                    view.SetRecoil(-st.RecoilDir * (RecoilDistance * k01));
                }

                if (st is { FlashElapsed: >= FlashDuration, RecoilElapsed: >= RecoilDuration })
                {
                    view.SetHitFlash(0f);
                    view.SetRecoil(Vector2.zero);
                    _hitFx.Remove(view);
                }
                else _hitFx[view] = st;
            }
        }
    }
}
