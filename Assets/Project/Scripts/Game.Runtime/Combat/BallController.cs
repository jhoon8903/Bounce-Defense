using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallController : BaseController
    {
        private const int MaxBounces = 40;
        // 실시간 연속 모델: 좌·우·천장 벽은 정상 반사, 바닥에 닿으면 반사 대신 Char(고정 원점)로 수집.
        // 바닥/비바닥 판정은 모터가 손실 없이 result.HitFloor/HitNonFloorWall로 넘겨준다(코너 다중바운스 안전).
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
        private readonly DamageResolver _resolver;
        private readonly CombatEventHub _hub;
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

        public BallController(IBallFactory factory, IClock clock, BallConfig config, DamageResolver resolver, CombatEventHub hub)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
            _resolver = resolver;
            _hub = hub;
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

                // 볼→데미지: 이번 스텝의 모든 적/블록 접촉에 데미지 파이프라인 실행(모터가 콜라이더 dedup).
                // 바닥 수집으로 continue하기 전에 처리 — 같은 스텝에 적+바닥을 맞아도 데미지는 살린다.
                ResolveDamageHits(id, model, motor);

                // 바닥면 반사면 반사 대신 수집(Char로 귀환) — 손실 없는 result.HitFloor 사용(코너 다중바운스에도 정확).
                // 비바닥 벽(좌·우·천장) 반사는 독립적으로 이벤트 발화. 한 스텝에 둘 다 일어나도 각각 정확히 처리.
                if (result.HitNonFloorWall) RaiseWallBounce(id, model, result.WallBouncePoint, result.WallBounceNormal);
                if (result.HitFloor) { _collecting.Add(id); continue; }
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
            // 발사 이벤트(주스/스킬 훅용). 현재 구독자 없어도 파이프라인 완결성 위해 발화.
            _hub?.RaiseLaunch(new BallLaunchInfo(BallSourceType.Normal, origin, direction));
            return model;
        }

        // 이번 스텝의 적/블록 접촉마다 데미지 파이프라인 실행 + OnHit 발화. 벽/바닥은 대상 아님.
        private void ResolveDamageHits(string id, BallModel model, IBallMotor motor)
        {
            if (_resolver == null) return;
            IReadOnlyList<MotorHit> hits = motor.LastStepHits;
            if (hits == null || hits.Count == 0) return;

            // 단일 반올림: 스폰 시 미리 반올림하지 않고, 원본 float 데미지를 파이프라인 끝(RoundDamageStage)에서 한 번만 반올림.
            float baseDamage = _config != null ? _config.GetDamage(1) : 0f;
            int ballInstanceId = _views.TryGetValue(id, out BallView view) && view != null ? view.GetInstanceID() : 0;

            for (int i = 0; i < hits.Count; i++)
            {
                MotorHit hit = hits[i];
                if (hit.Collider == null) continue;
                IDamageable target = hit.Collider.GetComponentInParent<IDamageable>();
                if (target == null) continue; // IDamageable 없는 대상(예: 브리지 미배선 블록) — 안전 무시.

                HitContext ctx = new HitContext(
                    DamageType.Direct,
                    model.SourceType,
                    target,
                    baseDamage,
                    canCrit: true,
                    canReceiveGlobalModifiers: true,
                    hitPosition: hit.Point,
                    hitNormal: hit.Normal,
                    sourceBallInstanceId: ballInstanceId);

                _resolver.Resolve(ctx);   // Base→Additive→Crit→Round→ApplyDamage(대상 HP 감소, 사망 시 RaiseKill).
                _hub?.RaiseHit(ctx);      // OnHit 구독자(주스/스킬 훅)에 알림.
            }
        }

        // 비바닥 벽(좌·우·천장) 반사 이벤트. point/normal은 모터가 캡처한 비바닥 벽 접촉값. 현재 구독자 없어도 완결성 위해 발화.
        private void RaiseWallBounce(string id, BallModel model, Vector2 point, Vector2 normal)
        {
            if (_hub == null) return;
            int ballInstanceId = _views.TryGetValue(id, out BallView view) && view != null ? view.GetInstanceID() : 0;
            _hub.RaiseWallBounce(new BallWallBounceInfo(ballInstanceId, model.SourceType, point, normal));
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
