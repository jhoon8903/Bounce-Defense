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
    // 전투 파티클 단일 스포너(HitFeedbackController와 SRP 분리: 그건 피격 반응=텍스트·플래시·움찔, 이건 파티클).
    //  - 볼 타입별 임팩트 풀 소유(BallFactory 패턴 미러: config별 Pool<ImpactVfxView>, 타입으로 키).
    //  - OnHit 구독 → 직격(hitDir != zero)에만 그 볼 타입 임팩트를 pos에 스폰. 2차뎀(번·행·폭발)은 hitDir=zero → 스킵.
    //  - 수명 전진 = IClock.OnTick(GameDeltaTime): 자체 Update 없음, 일시정지 정합(§11-9).
    //  - 향후 #3 죽음·#5 폭발·#6 번 파티클이 각자 훅과 함께 이 컨트롤러에 얹힘(파티클 스폰의 단일 집).
    public sealed class CombatVfxController : BaseController
    {
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;
        private readonly Dictionary<BallSourceType, Pool<ImpactVfxView>> _impactPools = new();
        private readonly Dictionary<ImpactVfxView, BallSourceType> _typeOf = new(); // 활성 인스턴스 → 스폰 타입(정확한 풀로 반환)
        private readonly List<ImpactVfxView> _active = new();
        private readonly BallSourceType _fallbackType;

        public CombatVfxController(IEnumerable<ImpactConfig> impactConfigs, Transform poolRoot, IClock clock, CombatEventHub hub)
        {
            _clock = clock;
            _hub = hub;
            BallSourceType first = BallSourceType.Normal;
            bool any = false;
            if (impactConfigs != null)
            {
                foreach (ImpactConfig c in impactConfigs)
                {
                    if (c == null || c.Prefab == null || _impactPools.ContainsKey(c.SourceType)) continue;
                    GameObject parentGo = new GameObject($"[Pool] {c.PoolName}");
                    if (poolRoot != null) parentGo.transform.SetParent(poolRoot);
                    _impactPools[c.SourceType] = new Pool<ImpactVfxView>(c, parentGo.transform);
                    if (!any) { first = c.SourceType; any = true; }
                }
            }
            // 폴백: Normal 풀 있으면 Normal, 없으면 처음 등록된 타입(임팩트 config 일부 미배선 시 다른 타입도 최소 표기).
            _fallbackType = _impactPools.ContainsKey(BallSourceType.Normal) ? BallSourceType.Normal : first;
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
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null) ReturnToPool(_active[i]);
            }
            _active.Clear();
            _typeOf.Clear();
        }

        protected override void OnReset() { }
        protected override void OnTick(float _) { }       // 실틱은 HandleTick(구독)
        protected override void OnFixedTick(float _) { }

        // 직격만 임팩트 스폰. 2차뎀은 hitDir=zero로 들어와 스킵(HitFeedback의 움찔 구분과 동일 규약).
        private void HandleHit(EnemyView view, Vector2 worldPos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType)
        {
            if (hitDir.sqrMagnitude <= 1e-6f) return;   // 번·행뎀·폭발 = 임팩트 없음
            if (_impactPools.Count == 0) return;         // 임팩트 프리팹 미배선 → 코어 루프 보존(무연출)
            BallSourceType useType = _impactPools.ContainsKey(sourceType) ? sourceType : _fallbackType;
            if (!_impactPools.TryGetValue(useType, out Pool<ImpactVfxView> pool)) return;
            ImpactVfxView fx = pool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(worldPos.x, worldPos.y, 0f));
            _typeOf[fx] = useType;
            _active.Add(fx);
        }

        // IClock.OnTick: 활성 임팩트 수명 전진(GameDeltaTime → 일시정지 시 dt=0으로 정지).
        private void HandleTick()
        {
            float dt = _clock.GameDeltaTime;
            if (dt <= 0f) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ImpactVfxView fx = _active[i];
                if (fx != null && fx.Tick(dt)) continue;
                if (fx != null) ReturnToPool(fx);
                _active.RemoveAt(i);
            }
        }

        // 스폰 타입을 되짚어 정확한 풀로 반환(엉뚱한 풀에 Release하면 그 풀의 active set이 오염됨).
        private void ReturnToPool(ImpactVfxView fx)
        {
            BallSourceType type = _typeOf.TryGetValue(fx, out BallSourceType t) ? t : _fallbackType;
            _typeOf.Remove(fx);
            if (_impactPools.TryGetValue(type, out Pool<ImpactVfxView> pool)) pool.Release(fx);
        }
    }
}
