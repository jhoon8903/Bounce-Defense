using System.Collections.Generic;
using Game.Combat;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Motor;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 lifecycle 오케스트레이터: 스폰/디스폰 명단 + 틱 구동만 담당.
    //  - '언제 쏘나'는 BallFiringScheduler, '어떻게 움직이나'는 IBallMotor, '데미지 규칙'은 DamageResolver 소유.
    // 실시간 연속 모델: 좌·우·천장 벽은 정상 반사, 바닥에 닿으면 반사 대신 Char(고정 원점)로 수집.
    public sealed class BallController : BaseController
    {
        private const int MaxBounces = 40;
        // 수집 도착 판정 거리(이 안쪽이면 Char에 닿은 것으로 보고 소멸).
        private const float CollectArrivalDist = 0.15f;
        private static readonly Vector2 OutOfBoundsMin = new(-6f, -11f);
        private static readonly Vector2 OutOfBoundsMax = new(6f, 11f);
        // Char 본체 월드 좌표(위치 고정). LaunchController가 SetCollectTarget으로 주입, 미배선 시 폴백.
        private static readonly Vector2 DefaultCollectTarget = new(0f, -6.70f);

        private readonly IBallFactory _factory;
        private readonly IClock _clock;
        private readonly BallConfig _config;
        private readonly DamageResolver _resolver;
        private readonly BallFiringScheduler _scheduler = new();
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

        public BallController(IBallFactory factory, IClock clock, BallConfig config, DamageResolver resolver)
        {
            _factory = factory;
            _clock = clock;
            _config = config;
            _resolver = resolver;
        }

        // Char 본체(고정 발사·수집 원점) 주입. 미호출 시 DefaultCollectTarget 사용.
        public void SetCollectTarget(Vector2 target) => _collectTarget = target;

        // ---- 연속 자동발사(스케줄러 위임). 시작 후 멈추지 않는다(실시간 연속발사 설계) ----
        public void StartFiring(Vector2 direction) => _scheduler.Start(direction);
        public void SetFireDirection(Vector2 direction) => _scheduler.SetDirection(direction);

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
            if (_scheduler.TryFire(fixedDeltaTime, _models.Count))
                Spawn(_collectTarget, _scheduler.Direction);
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

                // 볼→데미지: 이번 스텝의 모든 적/블록 접촉에 데미지 적용(모터가 콜라이더 dedup).
                // 바닥 수집으로 continue하기 전에 처리 — 같은 스텝에 적+바닥을 맞아도 데미지는 살린다.
                ResolveDamageHits(motor);

                // 바닥면 반사면 반사 대신 수집(Char로 귀환) — 손실 없는 result.HitFloor 사용(코너 다중바운스에도 정확).
                if (result.HitFloor) { _collecting.Add(id); continue; }
                if (IsOutOfBounds(motor.Position) || model.BounceCount >= MaxBounces) Release(id);
            }
        }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        public BallModel Spawn(Vector2 origin, Vector2 direction)
        {
            float speed = _config != null ? _config.Speed : 12f;
            float radius = _config != null ? _config.Radius : 0.15f;

            string id = _factory.GenerateId();
            (BallModel model, BallView view) = _factory.Create(origin);
            if (model == null || view == null) return null;

            IBallMotor motor = new KinematicRaycastMotor();
            motor.Init(origin, direction, speed, radius, _wallMask, _enemyMask, _blockMask, passThroughMask: 0);

            _models[id] = model;
            _views[id] = view;
            _motors[id] = motor;
            return model;
        }

        // 이번 스텝의 적/블록 접촉마다 데미지 적용. 벽/바닥은 대상 아님.
        // 단일 반올림: 스폰 시 미리 반올림하지 않고, 원본 float 데미지를 리졸버 끝에서 한 번만 반올림.
        private void ResolveDamageHits(IBallMotor motor)
        {
            if (_resolver == null) return;
            IReadOnlyList<Collider2D> hits = motor.LastStepHits;
            if (hits == null || hits.Count == 0) return;

            float baseDamage = _config != null ? _config.GetDamage(1) : 0f;
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] == null) continue;
                IDamageable target = hits[i].GetComponentInParent<IDamageable>();
                if (target == null) continue; // IDamageable 없는 대상(예: 브리지 미배선 블록) — 안전 무시.
                _resolver.Resolve(target, baseDamage);
            }
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
