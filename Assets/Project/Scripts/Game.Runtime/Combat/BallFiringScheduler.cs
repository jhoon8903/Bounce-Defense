using UnityEngine;

namespace Game.Runtime.Combat
{
    // 연속 자동발사의 '언제 쏘나' 단일 소유자: 발사 on/off·방향·간격 타이머·동시 비행 캡.
    // 실제 스폰(볼 생성)은 BallController가 한다 — 스케줄러는 타이밍 판정만.
    public sealed class BallFiringScheduler
    {
        // 발사 간격(초)과 동시 비행 최대 수("기본 5" = 최대 5개 멀티볼).
        private const float FireInterval = 0.12f;
        private const int MaxInFlight = 5;

        private bool _firing;
        private Vector2 _direction = Vector2.up;
        private float _timer;

        public Vector2 Direction => _direction;

        public void Start(Vector2 direction)
        {
            SetDirection(direction);
            _firing = true;
            _timer = FireInterval; // 첫 발은 다음 틱에 즉시.
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon) _direction = direction.normalized;
        }

        // 이번 틱에 1발 쏠지 판정. 발사 중이면 FireInterval마다 true(동시 비행 inFlight가 캡이면 대기).
        // 볼이 수집되어 슬롯이 비면 자동으로 다음 발사(재순환 스트림).
        public bool TryFire(float deltaTime, int inFlight)
        {
            if (!_firing) return false;
            _timer += deltaTime;
            if (_timer < FireInterval) return false;
            if (inFlight >= MaxInFlight) { _timer = FireInterval; return false; }
            _timer = 0f;
            return true;
        }
    }
}
