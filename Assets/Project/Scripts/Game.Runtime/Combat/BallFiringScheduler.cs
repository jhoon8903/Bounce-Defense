using UnityEngine;

namespace Game.Runtime.Combat
{
    // 연속 자동발사의 '언제 쏘나' 단일 소유자: 발사 on/off·방향·간격 타이머.
    // 동시 비행 캡은 로스터 크기(기본 5 + 액티브당 1)라 BallController가 매 틱 넘겨준다 — 스케줄러는 타이밍만 판정.
    // 실제 스폰(볼 생성)은 BallController가 한다.
    public sealed class BallFiringScheduler
    {
        private const float FireInterval = 0.12f; // 발사 간격(초).

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
        // maxInFlight = 현재 로스터 크기(기본 5 + 카드당 +1). 볼이 수집되어 슬롯이 비면 자동으로 다음 발사(재순환).
        public bool TryFire(float deltaTime, int inFlight, int maxInFlight)
        {
            if (!_firing) return false;
            _timer += deltaTime;
            if (_timer < FireInterval) return false;
            if (inFlight >= maxInFlight) { _timer = FireInterval; return false; }
            _timer = 0f;
            return true;
        }
    }
}
