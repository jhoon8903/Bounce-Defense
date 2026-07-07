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
    public sealed class CombatVfxController : BaseController
    {
        private readonly IClock _clock;
        private readonly CombatEventHub _hub;
        private readonly Dictionary<BallSourceType, Pool<ImpactVfxView>> _impactPools = new();
        private readonly Dictionary<ImpactVfxView, Pool<ImpactVfxView>> _poolOf = new();
        private readonly List<ImpactVfxView> _active = new();
        private readonly BallSourceType _fallbackType;
        private readonly Pool<ImpactVfxView> _explosionPool;
        private readonly Pool<ImpactVfxView> _clusterPool;
        private readonly Pool<ImpactVfxView> _deathPool;
        private readonly Pool<LaserBeamView> _laserPool;
        private readonly List<LaserBeamView> _activeLasers = new();
        private readonly Pool<ImpactVfxView> _bloodPool;

        public CombatVfxController(IEnumerable<ImpactConfig> impactConfigs, ImpactConfig explosionConfig, ImpactConfig clusterConfig, ImpactConfig deathConfig, ImpactConfig laserConfig, ImpactConfig bloodConfig, Transform poolRoot, IClock clock, CombatEventHub hub)
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
                    if (!any)
                    {
                        first = c.SourceType;
                        any = true;
                    }
                }
            }
            _fallbackType = _impactPools.ContainsKey(BallSourceType.Normal) ? BallSourceType.Normal : first;

            if (explosionConfig != null && explosionConfig.Prefab != null)
            {
                GameObject exGo = new GameObject($"[Pool] {explosionConfig.PoolName}");
                if (poolRoot != null) exGo.transform.SetParent(poolRoot);
                _explosionPool = new Pool<ImpactVfxView>(explosionConfig, exGo.transform);
            }
            if (clusterConfig != null && clusterConfig.Prefab != null)
            {
                GameObject clGo = new GameObject($"[Pool] {clusterConfig.PoolName}");
                if (poolRoot != null) clGo.transform.SetParent(poolRoot);
                _clusterPool = new Pool<ImpactVfxView>(clusterConfig, clGo.transform);
            }
            if (deathConfig != null && deathConfig.Prefab != null)
            {
                GameObject dGo = new GameObject($"[Pool] {deathConfig.PoolName}");
                if (poolRoot != null) dGo.transform.SetParent(poolRoot);
                _deathPool = new Pool<ImpactVfxView>(deathConfig, dGo.transform);
            }
            if (laserConfig != null && laserConfig.Prefab != null)
            {
                GameObject lzGo = new GameObject($"[Pool] {laserConfig.PoolName}");
                if (poolRoot != null) lzGo.transform.SetParent(poolRoot);
                _laserPool = new Pool<LaserBeamView>(laserConfig, lzGo.transform);
            }
            if (bloodConfig != null && bloodConfig.Prefab != null)
            {
                GameObject bGo = new GameObject($"[Pool] {bloodConfig.PoolName}");
                if (poolRoot != null) bGo.transform.SetParent(poolRoot);
                _bloodPool = new Pool<ImpactVfxView>(bloodConfig, bGo.transform);
            }
        }

        protected override void OnInitialize()
        {
            _hub.OnHit += HandleHit;
            _hub.OnExplosion += HandleExplosion;
            _hub.OnClusterBurst += HandleClusterBurst;
            _hub.OnEnemyDeath += HandleEnemyDeath;
            _hub.OnLaserRow += HandleLaserRow;
            _hub.OnBaseHit += HandleBaseHit;
            _clock.OnTick += HandleTick;
        }

        protected override void OnDispose()
        {
            _hub.OnHit -= HandleHit;
            _hub.OnExplosion -= HandleExplosion;
            _hub.OnClusterBurst -= HandleClusterBurst;
            _hub.OnEnemyDeath -= HandleEnemyDeath;
            _hub.OnLaserRow -= HandleLaserRow;
            _hub.OnBaseHit -= HandleBaseHit;
            _clock.OnTick -= HandleTick;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null) ReturnToPool(_active[i]);
            }
            _active.Clear();
            _poolOf.Clear();
            for (int i = 0; i < _activeLasers.Count; i++)
            {
                if (_activeLasers[i] != null) _laserPool.Release(_activeLasers[i]);
            }
            _activeLasers.Clear();
        }

        protected override void OnReset() { }
        protected override void OnTick(float _) { }
        protected override void OnFixedTick(float _) { }

        private void HandleHit(EnemyView view, Vector2 worldPos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType, DamageKind kind)
        {
            if (hitDir.sqrMagnitude <= 1e-6f) return;
            if (_impactPools.Count == 0) return;
            BallSourceType useType = _impactPools.ContainsKey(sourceType) ? sourceType : _fallbackType;
            if (!_impactPools.TryGetValue(useType, out Pool<ImpactVfxView> pool)) return;
            ImpactVfxView fx = pool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(worldPos.x, worldPos.y, 0f));
            _poolOf[fx] = pool;
            _active.Add(fx);
        }

        private void HandleExplosion(Vector2 center, float radius)
        {
            if (_explosionPool == null) return;
            ImpactVfxView fx = _explosionPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(center.x, center.y, 0f));
            _poolOf[fx] = _explosionPool;
            _active.Add(fx);
        }

        private void HandleClusterBurst(Vector2 center)
        {
            if (_clusterPool == null) return;
            ImpactVfxView fx = _clusterPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(center.x, center.y, 0f));
            _poolOf[fx] = _clusterPool;
            _active.Add(fx);
        }

        private void HandleEnemyDeath(Vector2 pos)
        {
            if (_deathPool == null) return;
            ImpactVfxView fx = _deathPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(pos.x, pos.y, 0f));
            _poolOf[fx] = _deathPool;
            _active.Add(fx);
        }

        private void HandleLaserRow(Vector2 center)
        {
            if (_laserPool == null) return;
            LaserBeamView beam = _laserPool.Get();
            if (beam == null) return;
            beam.Play(new Vector3(center.x, center.y, 0f));
            _activeLasers.Add(beam);
        }

        private void HandleBaseHit(Vector2 pos)
        {
            if (_bloodPool == null) return;
            ImpactVfxView fx = _bloodPool.Get();
            if (fx == null) return;
            fx.Play(new Vector3(pos.x, pos.y, 0f));
            _poolOf[fx] = _bloodPool;
            _active.Add(fx);
        }

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
