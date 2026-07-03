using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallController : BaseController
    {
        private const int MaxBounces = 40;
        private static readonly Vector2 OutOfBoundsMin = new(-6f, -11f);
        private static readonly Vector2 OutOfBoundsMax = new(6f, 11f);

        private readonly IBallFactory _factory;
        private readonly IClock _clock;
        private readonly BallConfig _config;
        private readonly Dictionary<string, BallModel> _models = new();
        private readonly Dictionary<string, BallView> _views = new();
        private readonly Dictionary<string, IBallMotor> _motors = new();
        private readonly List<string> _idCache = new();

        private int _wallMask;
        private int _enemyMask;
        private int _blockMask;

        public int ActiveCount => _models.Count;

        public BallController(IBallFactory factory, IClock clock, BallConfig config)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
        }

        protected override void OnInitialize()
        {
            _wallMask = LayerMask.GetMask("Wall");
            _enemyMask = LayerMask.GetMask("Enemy");
            _blockMask = LayerMask.GetMask("Block");
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
            if (_models.Count == 0) return;
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--)
            {
                string id = _idCache[i];
                if (!_motors.TryGetValue(id, out IBallMotor motor)) continue;
                if (!_models.TryGetValue(id, out BallModel model)) continue;

                BallMotorStepResult result = motor.Step(fixedDeltaTime);
                model.SetPosition(motor.Position);
                if (result.BounceCountThisStep > 0) model.RegisterBounce(result.BounceCountThisStep);

                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public void FireVolley(Vector2 origin, Vector2 direction, int count)
        {
            for (int i = 0; i < count; i++) Spawn(origin, direction);
        }

        public BallModel Spawn(Vector2 origin, Vector2 direction)
        {
            float speed = _config != null ? _config.Speed : 12f;
            float radius = _config != null ? _config.Radius : 0.15f;
            int damage = _config != null ? Mathf.RoundToInt(_config.GetDamage(1)) : 0;

            string id = _factory.GenerateId();
            (BallModel model, BallView view) = _factory.Create(id, origin, direction, speed, damage, BallSourceType.Normal);
            if (model == null || view == null) return null;

            IBallMotor motor = new KinematicRaycastMotor();
            motor.Init(origin, direction, speed, radius, _wallMask, _enemyMask, _blockMask, passThroughMask: 0);

            _models[id] = model;
            _views[id] = view;
            _motors[id] = motor;
            return model;
        }

        public void Release(string id)
        {
            if (!_models.TryGetValue(id, out BallModel model)) return;
            _views.TryGetValue(id, out BallView view);
            _factory.Release(model, view);
            _models.Remove(id);
            _views.Remove(id);
            _motors.Remove(id);
        }

        public void ReleaseAll()
        {
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--) Release(_idCache[i]);
        }

        private static bool IsOutOfBounds(Vector2 pos)
        {
            return pos.x < OutOfBoundsMin.x || pos.x > OutOfBoundsMax.x ||
                   pos.y < OutOfBoundsMin.y || pos.y > OutOfBoundsMax.y;
        }
    }
}
