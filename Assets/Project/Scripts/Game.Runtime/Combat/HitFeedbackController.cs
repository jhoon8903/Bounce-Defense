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
    // 피격 피드백 단일 오케스트레이터: (1) 풀링 데미지 숫자 스폰+틱, (2) 적 화이트 플래시 틱.
    // OnHit 구독(스폰/플래시 시작) + IClock.OnTick 구독(둘 다 GameDeltaTime으로 전진 → 일시정지 시 정지).
    public sealed class HitFeedbackController : BaseController
    {
        private const float FlashDuration = 0.2f; // 화이트→원복 블링크 시간
        private const float RecoilDuration = 0.3f; // 움찔 킥→원복 시간(§4)
        private const float RecoilDistance = 0.4f; // 몹 최대 밀림(월드 단위, 볼 반대방향). 격자/콜라이더는 불변

        private readonly IPool _pool;
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;

        private readonly List<DamageTextView> _activeTexts = new();
        private readonly Dictionary<EnemyView, HitFxState> _hitFx = new();
        private readonly List<EnemyView> _fxKeyCache = new();

        // 피격 순간 시작되는 뷰별 피드백 상태(플래시 + 움찔 반동). 같은 키·수명이라 하나로 묶어 고스트 가드를 공유한다.
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
        protected override void OnTick(float _) { }       // 실틱은 HandleTick(구독)
        protected override void OnFixedTick(float _) { }

        // ---- OnHit: 숫자 스폰 + 플래시/움찔 시작 ----
        // sourceType은 임팩트 파티클(CombatVfxController) 전용 — 여기선 미사용(피격 반응만 담당).
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
            // 살상타는 view가 이미 디스폰(무모델)될 수 있음 → 피드백 스킵. 숫자는 캡처된 pos로 계속 표기.
            if (view == null || !view.IsModelBound) return;
            HitFxState st = _hitFx.GetValueOrDefault(view);
            st.Model = view.Model;
            st.FlashElapsed = 0f; // 재피격이면 플래시 리셋(덮어씀)
            if (hitDir.sqrMagnitude > 1e-6f)
            {
                st.RecoilElapsed = 0f; 
                st.RecoilDir = hitDir.normalized;
            } // 직격 → 움찔 시작(재피격이면 리셋)
            else if (st.RecoilDir.sqrMagnitude < 1e-6f) st.RecoilElapsed = RecoilDuration; // 무방향(번·행뎀·폭발) & 진행중 반동 없음 → 반동 완료 처리
            _hitFx[view] = st;
        }

        // ---- IClock.OnTick: 숫자 + 플래시 전진(GameDeltaTime → 일시정지 시 dt=0) ----
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

                if (st.FlashElapsed < FlashDuration) // 화이트 플래시 1→0 감쇠
                {
                    st.FlashElapsed += dt;
                    view.SetHitFlash(st.FlashElapsed >= FlashDuration ? 0f : 1f - st.FlashElapsed / FlashDuration);
                }
                if (st.RecoilElapsed < RecoilDuration) // 움찔: 볼 반대방향(-법선)으로 킥 후 0으로 감쇠(몹만, 격자 불변)
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
