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
    //  - OnExplosion 구독 → Last Match 반경 폭발을 center에 1회 스폰(별도 _explosionPool, 반경 스케일). #5 완료.
    //  - 수명 전진 = IClock.OnTick(GameDeltaTime): 자체 Update 없음, 일시정지 정합(§11-9).
    //  - 향후 #3 죽음·#7 레이저 행 파티클이 각자 훅과 함께 이 컨트롤러에 얹힘(파티클 스폰의 단일 집).
    public sealed class CombatVfxController : BaseController
    {
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;
        private readonly Dictionary<BallSourceType, Pool<ImpactVfxView>> _impactPools = new();
        private readonly Dictionary<ImpactVfxView, Pool<ImpactVfxView>> _poolOf = new(); // 활성 인스턴스 → 소속 풀(정확히 반환; 임팩트·폭발 공통)
        private readonly List<ImpactVfxView> _active = new();
        private readonly BallSourceType _fallbackType;
        private readonly Pool<ImpactVfxView> _explosionPool; // Last Match 붉은 폭발(볼 타입 무관 단일 풀). 미배선 시 null → 무연출.
        private readonly Pool<ImpactVfxView> _clusterPool;   // Cluster 분열 = 수류탄 폭발. 미배선 시 null → 무연출.
        private readonly Pool<ImpactVfxView> _deathPool;     // 적 사망 = 돌 블럭 깨짐. 미배선 시 null → 무연출.
        private readonly Pool<LaserBeamView> _laserPool;     // Laser 행 빔(LineRenderer, 파티클 아님). 미배선 시 null → 무연출.
        private readonly List<LaserBeamView> _activeLasers = new(); // 활성 빔(ImpactVfxView와 타입 달라 별도 트래킹)

        public CombatVfxController(IEnumerable<ImpactConfig> impactConfigs, ImpactConfig explosionConfig, ImpactConfig clusterConfig, ImpactConfig deathConfig, ImpactConfig laserConfig, Transform poolRoot, IClock clock, CombatEventHub hub)
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

            // Last Match 폭발 풀(볼 타입 무관 단일). config·프리팹 미배선 시 폭발 무연출(코어 루프 보존).
            if (explosionConfig != null && explosionConfig.Prefab != null)
            {
                GameObject exGo = new GameObject($"[Pool] {explosionConfig.PoolName}");
                if (poolRoot != null) exGo.transform.SetParent(poolRoot);
                _explosionPool = new Pool<ImpactVfxView>(explosionConfig, exGo.transform);
            }
            // Cluster 분열 수류탄 폭발 풀. 미배선 시 무연출.
            if (clusterConfig != null && clusterConfig.Prefab != null)
            {
                GameObject clGo = new GameObject($"[Pool] {clusterConfig.PoolName}");
                if (poolRoot != null) clGo.transform.SetParent(poolRoot);
                _clusterPool = new Pool<ImpactVfxView>(clusterConfig, clGo.transform);
            }
            // 적 사망 돌 깨짐 풀. 미배선 시 무연출.
            if (deathConfig != null && deathConfig.Prefab != null)
            {
                GameObject dGo = new GameObject($"[Pool] {deathConfig.PoolName}");
                if (poolRoot != null) dGo.transform.SetParent(poolRoot);
                _deathPool = new Pool<ImpactVfxView>(deathConfig, dGo.transform);
            }
            // Laser 행 빔 풀(LineRenderer 뷰). 미배선 시 빔 무연출.
            if (laserConfig != null && laserConfig.Prefab != null)
            {
                GameObject lzGo = new GameObject($"[Pool] {laserConfig.PoolName}");
                if (poolRoot != null) lzGo.transform.SetParent(poolRoot);
                _laserPool = new Pool<LaserBeamView>(laserConfig, lzGo.transform);
            }
        }

        protected override void OnInitialize()
        {
            _hub.OnHit += HandleHit;
            _hub.OnExplosion += HandleExplosion;
            _hub.OnClusterBurst += HandleClusterBurst;
            _hub.OnEnemyDeath += HandleEnemyDeath;
            _hub.OnLaserRow += HandleLaserRow;
            _clock.OnTick += HandleTick;
        }

        protected override void OnDispose()
        {
            _hub.OnHit -= HandleHit;
            _hub.OnExplosion -= HandleExplosion;
            _hub.OnClusterBurst -= HandleClusterBurst;
            _hub.OnEnemyDeath -= HandleEnemyDeath;
            _hub.OnLaserRow -= HandleLaserRow;
            _clock.OnTick -= HandleTick;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null) ReturnToPool(_active[i]);
            }
            _active.Clear();
            _poolOf.Clear();
            for (int i = 0; i < _activeLasers.Count; i++)
                if (_activeLasers[i] != null) _laserPool.Release(_activeLasers[i]);
            _activeLasers.Clear();
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
            _poolOf[fx] = pool;
            _active.Add(fx);
        }

        // Last Match 반경 폭발: center에 붉은 폭발 1회 스폰. 크기는 폭발 프리팹 자체(transform·PS 내부)에서 정함
        // — 코드가 transform scale을 건드리지 않는다(Daniel 규칙). radius는 향후 내부 스케일 튜닝용으로만 전달됨(현재 미사용).
        private void HandleExplosion(Vector2 center, float radius)
        {
            if (_explosionPool == null) return;
            ImpactVfxView fx = _explosionPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(center.x, center.y, 0f));
            _poolOf[fx] = _explosionPool;
            _active.Add(fx);
        }

        // Cluster 분열 트리거: 위치에 수류탄 폭발 1회 스폰(특수볼이 파편처럼 튀어나옴). 크기는 프리팹 자체. 미배선 시 스킵.
        private void HandleClusterBurst(Vector2 center)
        {
            if (_clusterPool == null) return;
            ImpactVfxView fx = _clusterPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(center.x, center.y, 0f));
            _poolOf[fx] = _clusterPool;
            _active.Add(fx);
        }

        // 적 사망: 사망 위치에 돌 블럭 깨짐 1회 스폰. 모든 킬 경로 공통. 미배선 시 스킵.
        private void HandleEnemyDeath(Vector2 pos)
        {
            if (_deathPool == null) return;
            ImpactVfxView fx = _deathPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(pos.x, pos.y, 0f));
            _poolOf[fx] = _deathPool;
            _active.Add(fx);
        }

        // Laser 행뎀: 행 월드좌표(center)에 가로 LineRenderer 빔 1회 스폰. 폭은 빔 프리팹 자체가 9칸으로 authored. 미배선 시 스킵.
        private void HandleLaserRow(Vector2 center)
        {
            if (_laserPool == null) return;
            LaserBeamView beam = _laserPool.Get();
            if (beam == null) return;
            beam.Play(new Vector3(center.x, center.y, 0f));
            _activeLasers.Add(beam);
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
            for (int i = _activeLasers.Count - 1; i >= 0; i--)
            {
                LaserBeamView b = _activeLasers[i];
                if (b != null && b.Tick(dt)) continue;
                if (b != null) _laserPool.Release(b);
                _activeLasers.RemoveAt(i);
            }
        }

        // 소속 풀로 정확히 반환(엉뚱한 풀에 Release하면 그 풀의 active set이 오염됨). 임팩트·폭발 공통.
        private void ReturnToPool(ImpactVfxView fx)
        {
            if (_poolOf.TryGetValue(fx, out Pool<ImpactVfxView> pool))
            {
                _poolOf.Remove(fx);
                pool.Release(fx);
            }
        }
    }
}
