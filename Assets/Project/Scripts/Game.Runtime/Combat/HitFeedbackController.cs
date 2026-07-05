using System.Collections.Generic;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Core.Pool;
using Game.Events;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 피격 피드백 단일 오케스트레이터: (1) 풀링 데미지 숫자 스폰+틱, (2) 적 화이트 플래시 틱.
    // OnHit 구독(스폰/플래시 시작) + IClock.OnTick 구독(둘 다 GameDeltaTime으로 전진 → 일시정지 시 정지).
    public sealed class HitFeedbackController : BaseController
    {
        private const float FlashDuration = 0.14f; // 화이트→원복 블링크 시간

        private readonly IPool _pool;
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;

        private readonly List<DamageTextView> _activeTexts = new();
        private readonly Dictionary<EnemyView, FlashState> _flashes = new();
        private readonly List<EnemyView> _flashKeyCache = new();

        private struct FlashState { public float Elapsed; public EnemyModel Model; }

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
                if (_activeTexts[i] != null) _pool.Return(_activeTexts[i]);
            _activeTexts.Clear();
            _flashes.Clear();
        }

        protected override void OnReset() { }
        protected override void OnTick(float _) { }       // 실틱은 HandleTick(구독)
        protected override void OnFixedTick(float _) { }

        // ---- OnHit: 숫자 스폰 + 플래시 시작 ----
        private void HandleHit(EnemyView view, Vector2 worldPos, int amount, bool isCrit)
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
            // 살상타는 view가 이미 디스폰(무모델)될 수 있음 → 플래시 스킵. 숫자는 캡처된 pos로 계속 표기.
            if (view != null && view.IsModelBound)
                _flashes[view] = new FlashState { Elapsed = 0f, Model = view.Model }; // 재피격이면 리셋(덮어씀)
        }

        // ---- IClock.OnTick: 숫자 + 플래시 전진(GameDeltaTime → 일시정지 시 dt=0) ----
        private void HandleTick()
        {
            float dt = _clock.GameDeltaTime;
            if (dt <= 0f) return;

            for (int i = _activeTexts.Count - 1; i >= 0; i--)
            {
                DamageTextView t = _activeTexts[i];
                if (t == null || !t.Tick(dt))
                {
                    if (t != null) _pool.Return(t);
                    _activeTexts.RemoveAt(i);
                }
            }

            if (_flashes.Count == 0) return;
            _flashKeyCache.Clear();
            _flashKeyCache.AddRange(_flashes.Keys);
            for (int k = 0; k < _flashKeyCache.Count; k++)
            {
                EnemyView view = _flashKeyCache[k];
                if (!_flashes.TryGetValue(view, out FlashState st)) continue;
                // 고스트 가드: 풀 재발급으로 다른 적이 된 인스턴스는 절대 건드리지 않음.
                if (view == null || !view.IsModelBound || !ReferenceEquals(view.Model, st.Model))
                {
                    _flashes.Remove(view);
                    continue;
                }
                st.Elapsed += dt;
                if (st.Elapsed >= FlashDuration)
                {
                    view.SetHitFlash(0f);
                    _flashes.Remove(view);
                }
                else
                {
                    view.SetHitFlash(1f - st.Elapsed / FlashDuration); // 1→0 감쇠
                    _flashes[view] = st;
                }
            }
        }
    }
}
