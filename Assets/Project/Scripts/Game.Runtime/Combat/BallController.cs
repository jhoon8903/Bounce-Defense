using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Core.Random;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallController : BaseController, IBallEffectContext
    {
        private const int MaxBounces = 40;
        private const int DefaultBallCount = 5;
        private const float CollectArrivalDist = 0.15f;
        private static readonly Vector2 OutOfBoundsMin = new(-6f, -11f);
        private static readonly Vector2 OutOfBoundsMax = new(6f, 11f);
        private static readonly Vector2 DefaultCollectTarget = new(0f, -6.70f);

        private readonly IBallFactory _factory;
        private readonly IClock _clock;
        private readonly BallConfig _config;
        private readonly DamageResolver _resolver;
        private readonly CombatEventHub _hub;
        private readonly GridController _grid;
        private readonly BallFiringScheduler _scheduler = new();
        private readonly Dictionary<string, BallModel> _models = new();
        private readonly Dictionary<string, BallView> _views = new();
        private readonly Dictionary<string, IBallMotor> _motors = new();
        private readonly Dictionary<string, BallSpawnSpec> _specs = new();
        private readonly Stack<KinematicRaycastMotor> _motorPool = new();
        private readonly HashSet<string> _collecting = new();
        private readonly List<string> _idCache = new();
        private readonly HashSet<int> _rowHitHandles = new();
        private readonly HashSet<string> _unmanaged = new();
        private readonly HashSet<string> _mirrorArmed = new();
        private float _mirrorPercent;

        private readonly List<BallSpawnSpec> _roster = new();
        private readonly Dictionary<BallSourceType, int> _desiredByType = new();
        private readonly Dictionary<BallSourceType, int> _inFlightByType = new();

        private int _wallMask;
        private int _enemyMask;
        private int _blockMask;
        private Vector2 _collectTarget = DefaultCollectTarget;

        public BallController(IBallFactory factory, IClock clock, BallConfig config, DamageResolver resolver, CombatEventHub hub, IRandom random, GridController grid)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
            _resolver = resolver;
            _hub = hub;
            Random = random;
            _grid = grid;
        }

        public IRandom Random { get; }

        public void DamageEnemyRow(IDamageable originEnemy, float flatDamage, BallSourceType source)
        {
            if (_resolver == null || _grid is not { IsReady: true } || flatDamage <= 0f) return;
            if (originEnemy is not EnemyView originView || originView.Model == null) return;
            int row = _grid.Model.WorldToCell(originView.Model.Position).Row;
            int cols = _grid.Cols;
            _hub?.RaiseLaserRow(new Vector2(0f, _grid.CellToWorld(0, row).y));
            _rowHitHandles.Clear();
            for (int col = 0; col < cols; col++)
            {
                int handle = _grid.OccupantHandleAt(col, row);
                if (handle == GridMap.Empty || !_rowHitHandles.Add(handle)) continue;
                if (!_grid.TryGetOccupant(handle, out IDamageable occ) || occ == originEnemy) continue;
                Vector2 pos = occ is EnemyView { Model: not null } ev ? ev.Model.Position : Vector2.zero;
                HitContext ctx = HitContext.Secondary(occ, source, DamageKind.LaserRow, flatDamage);
                _resolver.Resolve(ref ctx);
                if (ctx.FinalDamage > 0 && occ is EnemyView ev2) _hub?.RaiseHit(ev2, pos, ctx.FinalDamage, ctx.IsCrit, Vector2.zero, source, ctx.Kind);
            }
        }

        public void SetMirrorPercent(float percent) => _mirrorPercent = Mathf.Max(0f, percent);

        public void SetCollectTarget(Vector2 target) => _collectTarget = target;

        public void StartFiring(Vector2 direction) => _scheduler.Start(direction);
        public void SetFireDirection(Vector2 direction) => _scheduler.SetDirection(direction);

        public void SetRoster(IReadOnlyList<BallSpawnSpec> specs)
        {
            _roster.Clear();
            if (specs is { Count: > 0 }) _roster.AddRange(specs);
            else BuildDefaultRoster();
            RecomputeDesired();
        }

        protected override void OnInitialize()
        {
            _wallMask = LayerMask.GetMask("Wall");
            _enemyMask = LayerMask.GetMask("Enemy");
            _blockMask = LayerMask.GetMask("Block");
            if (_roster.Count == 0)
            {
                BuildDefaultRoster();
                RecomputeDesired();
            }
            _clock.OnFixedTick += OnClockFixedTick;
        }

        protected override void OnDispose()
        {
            _clock.OnFixedTick -= OnClockFixedTick;
            ReleaseAll();
        }

        protected override void OnReset() => ReleaseAll();

        protected override void OnTick(float deltaTime) { }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_scheduler.TryFire(fixedDeltaTime, _models.Count, _roster.Count) && TryPickSpec(out BallSpawnSpec spec)) Spawn(_collectTarget, _scheduler.Direction, spec);
            if (_models.Count == 0) return;
            float collectSpeed = _config != null ? _config.Speed : 12f;
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--)
            {
                string id = _idCache[i];
                if (!_models.TryGetValue(id, out BallModel model)) continue;
                if (_collecting.Contains(id))
                {
                    Vector2 toTarget = _collectTarget - model.Position;
                    float step = collectSpeed * fixedDeltaTime;
                    if (toTarget.magnitude <= Mathf.Max(step, CollectArrivalDist)) Release(id);
                    else model.SetPosition(model.Position + toTarget.normalized * step);
                    continue;
                }

                if (!_motors.TryGetValue(id, out IBallMotor motor)) continue;
                BallMotorStepResult result = motor.Step(fixedDeltaTime);
                model.SetPosition(motor.Position);
                if (result.BounceCountThisStep > 0) model.RegisterBounce(result.BounceCountThisStep);
                ResolveDamageHits(id, motor);
                if (_mirrorPercent > 0f && result.WallBounceCountThisStep > 0) _mirrorArmed.Add(id);
                if (result.HitFloor)
                {
                    _collecting.Add(id);
                    continue;
                }
                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public int ShotsFired { get; private set; }

        public BallModel Spawn(Vector2 origin, Vector2 direction, BallSpawnSpec spec)
        {
            ShotsFired++;
            return SpawnInternal(origin, direction, spec, managed: true);
        }

        public void SpawnClusterBall(Vector2 origin, float damage)
        {
            if (damage <= 0f) return;
            _hub?.RaiseClusterBurst(origin);
            float t = Random?.NextFloat() ?? 0.5f;
            float angle = Mathf.Deg2Rad * (20f + t * 140f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            BallSpawnSpec spec = new BallSpawnSpec(BallSourceType.Cluster, damage, null, false, DamageKind.ClusterSpawn);
            SpawnInternal(origin, dir, spec, managed: false);
        }

        private BallModel SpawnInternal(Vector2 origin, Vector2 direction, BallSpawnSpec spec, bool managed)
        {
            float speed = _config != null ? _config.Speed : 12f;
            float radius = _config != null ? _config.Radius : 0.15f;
            string id = _factory.GenerateId();
            (BallModel model, BallView view) = _factory.Create(origin, spec.SourceType);
            if (model == null || view == null) return null;
            int passThroughMask = spec.PenetratesEnemies ? _enemyMask : 0;
            KinematicRaycastMotor motor = _motorPool.Count > 0 ? _motorPool.Pop() : new KinematicRaycastMotor();
            motor.Init(origin, direction, speed, radius, _wallMask, _enemyMask, _blockMask, passThroughMask);
            _models[id] = model;
            _views[id] = view;
            _motors[id] = motor;
            _specs[id] = spec;
            if (managed) _inFlightByType[spec.SourceType] = InFlightOf(spec.SourceType) + 1;
            else _unmanaged.Add(id);
            return model;
        }

        private void ResolveDamageHits(string id, IBallMotor motor)
        {
            if (_resolver == null) return;
            IReadOnlyList<BallHit> hits = motor.LastStepHits;
            if (hits == null || hits.Count == 0) return;

            BallSpawnSpec spec = _specs.TryGetValue(id, out BallSpawnSpec s) ? s : DefaultSpec();
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D collider = hits[i].Collider;
                if (collider == null) continue;
                Vector2 pos = hits[i].Point;
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target == null) continue;
                HitContext ctx = spec.DamageKind == DamageKind.Direct
                    ? HitContext.Direct(target, spec.SourceType, spec.BaseDamage, hits[i].Normal)
                    : HitContext.Secondary(target, spec.SourceType, spec.DamageKind, spec.BaseDamage);
                if (spec.DamageKind == DamageKind.Direct && _mirrorArmed.Remove(id))
                    ctx.BonusAdditivePercent = _mirrorPercent;
                _resolver.Resolve(ref ctx);
                if (target is EnemyView ev) _hub?.RaiseHit(ev, pos, ctx.FinalDamage, ctx.IsCrit, hits[i].Normal, spec.SourceType, ctx.Kind);
                spec.Module?.OnEnemyHit(target, ctx, this);
            }
        }

        public void Release(string id)
        {
            if (!_models.TryGetValue(id, out BallModel model)) return;
            _views.TryGetValue(id, out BallView view);
            _factory.Release(model, view);
            if (!_unmanaged.Remove(id) && _specs.TryGetValue(id, out BallSpawnSpec spec)) _inFlightByType[spec.SourceType] = Mathf.Max(0, InFlightOf(spec.SourceType) - 1);
            if (_motors.TryGetValue(id, out IBallMotor usedMotor) && usedMotor is KinematicRaycastMotor krm) _motorPool.Push(krm);
            _models.Remove(id);
            _views.Remove(id);
            _motors.Remove(id);
            _specs.Remove(id);
            _collecting.Remove(id);
            _mirrorArmed.Remove(id);
        }

        public void ReleaseAll()
        {
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--) Release(_idCache[i]);
        }

        private void BuildDefaultRoster()
        {
            _roster.Clear();
            BallSpawnSpec normal = DefaultSpec();
            for (int i = 0; i < DefaultBallCount; i++) _roster.Add(normal);
        }

        private BallSpawnSpec DefaultSpec() => new(BallSourceType.Normal, 0f, null);

        private void RecomputeDesired()
        {
            _desiredByType.Clear();
            for (int i = 0; i < _roster.Count; i++)
            {
                BallSourceType t = _roster[i].SourceType;
                _desiredByType[t] = (_desiredByType.GetValueOrDefault(t, 0)) + 1;
            }
        }

        private bool TryPickSpec(out BallSpawnSpec spec)
        {
            for (int i = 0; i < _roster.Count; i++)
            {
                BallSourceType t = _roster[i].SourceType;
                int desired = _desiredByType.GetValueOrDefault(t, 0);
                if (InFlightOf(t) >= desired) continue;
                spec = _roster[i];
                return true;
            }
            spec = default;
            return false;
        }

        private int InFlightOf(BallSourceType type) => _inFlightByType.GetValueOrDefault(type, 0);

        private static bool IsOutOfBounds(Vector2 pos)
        {
            return pos.x < OutOfBoundsMin.x || pos.x > OutOfBoundsMax.x ||
                   pos.y < OutOfBoundsMin.y || pos.y > OutOfBoundsMax.y;
        }
    }
}
