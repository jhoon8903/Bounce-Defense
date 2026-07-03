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
        // 바닥(아레나 하단 벽) 반사의 노멀은 위(+y)를 향한다. 이 임계 이상이면 바닥으로 판정.
        // 실시간 연속 모델: 좌·우·천장 벽은 정상 반사, 바닥에 닿으면 반사 대신 Char(고정 원점)로 수집.
        private const float FloorNormalThreshold = 0.7f;
        // 수집 도착 판정 거리(이 안쪽이면 Char에 닿은 것으로 보고 소멸).
        private const float CollectArrivalDist = 0.15f;
        private static readonly Vector2 OutOfBoundsMin = new(-6f, -11f);
        private static readonly Vector2 OutOfBoundsMax = new(6f, 11f);
        // Char 본체 월드 좌표(위치 고정). GameLifetimeScope에서 SetCollectTarget으로 주입, 미배선 시 폴백.
        private static readonly Vector2 DefaultCollectTarget = new(0f, -6.70f);
        // 연속 자동발사: 발사 간격(초)과 동시 비행 최대 수("기본 5" = 최대 5개 멀티볼).
        private const float FireInterval = 0.12f;
        private const int MaxInFlight = 5;

        private readonly IBallFactory _factory;
        private readonly IClock _clock;
        private readonly BallConfig _config;
        private readonly Dictionary<string, BallModel> _models = new();
        private readonly Dictionary<string, BallView> _views = new();
        private readonly Dictionary<string, IBallMotor> _motors = new();
        // 바닥을 맞고 Char로 귀환 중인 볼(모터 스텝 대신 직선 홈잉). 도착 시 소멸.
        private readonly HashSet<string> _collecting = new();
        private readonly List<string> _idCache = new();

        private int _wallMask;
        private int _enemyMask;
        private int _blockMask;
        private Vector2 _collectTarget = DefaultCollectTarget;

        private bool _firing;
        private Vector2 _fireDir = Vector2.up;
        private float _fireTimer;

        public int ActiveCount => _models.Count;
        public bool IsFiring => _firing;

        // Char 본체(고정 발사·수집 원점) 주입. 미호출 시 DefaultCollectTarget 사용.
        public void SetCollectTarget(Vector2 target) => _collectTarget = target;

        // 연속 자동발사 시작/방향변경/정지. 발사 중엔 FireInterval마다 Char에서 _fireDir로 볼을 쏜다(최대 MaxInFlight).
        public void StartFiring(Vector2 direction)
        {
            SetFireDirection(direction);
            _firing = true;
            _fireTimer = FireInterval; // 첫 발은 다음 틱에 즉시.
        }

        public void SetFireDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon) _fireDir = direction.normalized;
        }

        public void StopFiring() => _firing = false;

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
            UpdateFiring(fixedDeltaTime);
            if (_models.Count == 0) return;
            float collectSpeed = _config != null ? _config.Speed : 12f;
            _idCache.Clear();
            _idCache.AddRange(_models.Keys);
            for (int i = _idCache.Count - 1; i >= 0; i--)
            {
                string id = _idCache[i];
                if (!_models.TryGetValue(id, out BallModel model)) continue;

                // 수집 중: 모터 물리 대신 Char로 직선 홈잉, 도착 시 소멸.
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

                // 바닥에 닿으면 반사 대신 수집 상태로 전환(Char로 귀환). 좌·우·천장은 정상 반사.
                bool hitFloor = result.HitWall && result.HitNormal.y >= FloorNormalThreshold;
                if (hitFloor) { _collecting.Add(id); continue; }
                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        // 연속 자동발사: 발사 중이면 FireInterval마다 Char 원점에서 _fireDir로 1발. 동시 비행 MaxInFlight로 캡.
        // 볼이 수집되어 슬롯이 비면 자동으로 다음 발사(재순환 스트림).
        private void UpdateFiring(float fixedDeltaTime)
        {
            if (!_firing) return;
            _fireTimer += fixedDeltaTime;
            if (_fireTimer < FireInterval) return;
            if (_models.Count >= MaxInFlight) { _fireTimer = FireInterval; return; }
            _fireTimer = 0f;
            Spawn(_collectTarget, _fireDir);
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
            _collecting.Remove(id);
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
