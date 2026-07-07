using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyController : BaseController
    {
        private readonly IEnemyFactory _factory;
        private readonly IClock _clock;
        private readonly GridController _grid;
        private readonly CombatEventHub _hub;
        private readonly DamageResolver _resolver;

        private readonly Dictionary<string, EnemyModel> _models = new();
        private readonly Dictionary<string, EnemyView> _views = new();
        private readonly Dictionary<string, int> _handles = new();
        private readonly List<string> _idCache = new();

        private readonly EnemyEntranceChoreographer _entrance;
        private readonly EnemyDescentSimulator _descent;
        private readonly BreachChoreographer _breach;
        private readonly EnemyStatusSimulator _status;
        private readonly System.Action<EnemyView, int> _damageSink;
        private readonly System.Action<EnemyView, float, float, int> _burnSink;
        private readonly System.Action<EnemyView, float, float> _freezeSink;
        private Vector2 _defensePoint = new Vector2(0f, -6.70f);

        private const int MaxExplosionDepth = 4;
        private float _lastMatchDamage;
        private float _lastMatchRadius;
        private int _explosionDepth;
        private readonly List<int>[] _explosionHits;

        public EnemyController(IEnemyFactory factory, IClock clock, GridController grid, CombatEventHub hub, DamageResolver resolver)
        {
            _factory = factory;
            _clock = clock;
            _grid = grid;
            _hub = hub;
            _resolver = resolver;
            _damageSink = HandleDamage;
            _burnSink = HandleBurn;
            _freezeSink = HandleFreeze;
            _entrance = new EnemyEntranceChoreographer();
            _descent = new EnemyDescentSimulator(grid, _models, _views, _handles, OnDescentBreach);
            _breach = new BreachChoreographer(OnBreachImpact);
            _status = new EnemyStatusSimulator(ApplyBurnDamage, SetFreezeSlow, SetEnemyBurning, SetEnemyFrozen);
            _explosionHits = new List<int>[MaxExplosionDepth];
            for (int i = 0; i < MaxExplosionDepth; i++)
            {
                _explosionHits[i] = new List<int>();
            }
        }

        protected override void OnInitialize() => _clock.OnFixedTick += OnClockFixedTick;

        protected override void OnDispose()
        {
            _clock.OnFixedTick -= OnClockFixedTick;
            DespawnAll();
        }

        protected override void OnReset() => DespawnAll();
        protected override void OnTick(float deltaTime) { }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public EnemyModel Spawn(EnemyDefinition definition, CellCoord anchor, int cascadeIndex, float hpScale = 1f)
        {
            if (definition == null || _grid == null || !_grid.IsReady) return null;
            Footprint fp = definition.Footprint;
            if (!_grid.Model.CanPlace(anchor, fp)) return null;

            Vector2 center = _grid.Model.FootprintWorldCenter(anchor, fp);
            string id = _factory.GenerateId();
            (EnemyModel model, EnemyView view) = _factory.Create(id, definition, center, hpScale);
            if (model == null || view == null) return null;

            if (!_grid.TryPlaceBlock(anchor, fp, out BlockPlacement placement, view))
            {
                _factory.Release(model, view);
                return null;
            }

            view.SetFootprintSize(placement.WorldSize);
            view.SetDamageSink(_damageSink);
            view.SetBurnSink(_burnSink);
            view.SetFreezeSink(_freezeSink);

            _models[id] = model;
            _views[id] = view;
            _handles[id] = placement.Handle;

            _entrance.Begin(id, model, view, cascadeIndex, center, placement.WorldSize);
            return model;
        }

        public bool CanSpawnAt(EnemyDefinition definition, CellCoord anchor)
        {
            if (definition == null || _grid is not { IsReady: true }) return false;
            return _grid.Model.CanPlace(anchor, definition.Footprint);
        }

        public void SetLastMatch(float damage, float radius)
        {
            _lastMatchDamage = damage;
            _lastMatchRadius = radius;
        }

        public void HandleDamage(EnemyView view, int amount)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return;

            model.TakeDamage(amount);
            if (!model.IsDead) return;

            Vector2 deathPos = model.Position;
            BlockPlacement deadPlacement = default;
            bool hasPlacement = _handles.TryGetValue(model.Id, out int deadHandle)
                && _grid.TryGetPlacement(deadHandle, out deadPlacement);
            _hub?.RaiseKill();
            _hub?.RaiseEnemyDeath(deathPos);
            Despawn(model.Id);
            if (hasPlacement) TryLastMatchExplosion(deadPlacement.Anchor, deadPlacement.Footprint, deathPos);
        }

        private void TryLastMatchExplosion(CellCoord anchor, Footprint fp, Vector2 center)
        {
            if (_lastMatchDamage <= 0f || _resolver == null) return;
            if (_explosionDepth >= MaxExplosionDepth) return;
            _explosionDepth++;

            _hub?.RaiseExplosion(center, _lastMatchRadius);

            List<int> hits = _explosionHits[_explosionDepth - 1];
            hits.Clear();
            int c0 = anchor.Col, r0 = anchor.Row;
            int w = Mathf.Max(1, fp.Width), h = Mathf.Max(1, fp.Height);
            for (int c = c0 - 1; c <= c0 + w; c++)
            for (int r = r0 - 1; r <= r0 + h; r++)
            {
                if (c >= c0 && c < c0 + w && r >= r0 && r < r0 + h) continue;
                if (c < 0 || c >= _grid.Cols || r < 0 || r >= _grid.Rows) continue;
                int occ = _grid.OccupantHandleAt(c, r);
                if (occ != GridMap.Empty && !hits.Contains(occ)) hits.Add(occ);
            }

            for (int i = 0; i < hits.Count; i++)
            {
                if (!_grid.TryGetOccupant(hits[i], out IDamageable dmg) || dmg is not EnemyView v || v.Model == null) continue;
                if (v.Model.IsDead || v.Model.IsEntering) continue;
                Vector2 pos = v.Model.Position;
                HitContext ctx = HitContext.Secondary(v, BallSourceType.Normal, DamageKind.Explosion, _lastMatchDamage);
                _resolver.Resolve(ref ctx);
                if (ctx.FinalDamage > 0) _hub?.RaiseHit(v, pos, ctx.FinalDamage, false, Vector2.zero, BallSourceType.Normal, ctx.Kind);
            }

            _explosionDepth--;
        }

        private void HandleBurn(EnemyView view, float duration, float dps, int maxStacks)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return;
            _status.ApplyBurn(model.Id, duration, dps, maxStacks);
        }

        private void HandleFreeze(EnemyView view, float duration, float slow)
        {
            if (view == null) return;
            EnemyModel model = view.Model;
            if (model == null || model.IsDead || model.IsEntering) return;
            _status.ApplyFreeze(model.Id, duration, slow);
        }

        private void SetFreezeSlow(string id, float slow)
        {
            if (_models.TryGetValue(id, out EnemyModel model) && model != null) model.SetFreezeSlow(slow);
        }

        private void SetEnemyBurning(string id, bool on)
        {
            if (_views.TryGetValue(id, out EnemyView view) && view != null) view.SetBurning(on);
        }

        private void SetEnemyFrozen(string id, bool on)
        {
            if (_views.TryGetValue(id, out EnemyView view) && view != null) view.SetFrozen(on);
        }

        private void ApplyBurnDamage(string id, float dps)
        {
            if (_resolver == null || !_views.TryGetValue(id, out EnemyView view) || view.Model == null) return;
            Vector2 pos = view.Model.Position;
            HitContext ctx = HitContext.Secondary(view, BallSourceType.Fire, DamageKind.Burn, dps);
            _resolver.Resolve(ref ctx);
            if (ctx.FinalDamage > 0) _hub?.RaiseHit(view, pos, ctx.FinalDamage, ctx.IsCrit, Vector2.zero, BallSourceType.Fire, ctx.Kind);
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            _entrance.Tick(fixedDeltaTime);
            _descent.Tick(fixedDeltaTime);
            _breach.Tick(fixedDeltaTime);
            _status.Tick(fixedDeltaTime);
        }

        private void OnDescentBreach(string id, int breachDamage)
        {
            if (_handles.TryGetValue(id, out int h))
            {
                _grid.RemoveBlock(h);
                _handles.Remove(id);
            }
            if (_models.TryGetValue(id, out EnemyModel m) && _views.TryGetValue(id, out EnemyView v))
            {
                m.BeginBreaching();
                v.SetColliderEnabled(false);
                _breach.Begin(id, m, v, breachDamage, _defensePoint);
            }
            else Despawn(id);
        }

        private void OnBreachImpact(string id, int breachDamage, Vector2 impactPos)
        {
            _hub?.RaiseHit(null, impactPos, breachDamage, true, Vector2.zero, BallSourceType.Normal, Game.Combat.DamageKind.Direct);
            _hub?.RaiseBaseHit(impactPos);
            _hub?.RaiseBreach(breachDamage);
            Despawn(id);
        }

        public void SetDefensePoint(Vector2 point) => _defensePoint = point;

        public void Despawn(string id)
        {
            if (!_models.TryGetValue(id, out EnemyModel model)) return;
            _views.TryGetValue(id, out EnemyView view);
            if (_handles.TryGetValue(id, out int handle)) _grid.RemoveBlock(handle);
            _entrance.Remove(id);
            _breach.Remove(id);
            _status.Remove(id);
            _factory.Release(model, view);
            _models.Remove(id);
            _views.Remove(id);
            _handles.Remove(id);
        }

        public void DespawnAll()
        {
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--)
            {
                Despawn(_idCache[i]);
            }
        }
    }
}
