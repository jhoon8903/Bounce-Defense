using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    // 발사대(사신) = 발사 권한 + 고정 원점의 단일 소유자.
    // 책임: (1) 발사·수집 원점(Char 본체)을 소유하고 BallController에 주입한다(발사 스폰 + 바닥 수집 공용).
    //       (2) 발사 라이프사이클(시작/조준/정지)을 BallController에 위임한다.
    // 입력(조준)은 AimController가 담당하고 여기로만 위임한다 → 단방향 의존: AimController → LaunchController → BallController.
    public sealed class LaunchController : MonoBehaviour
    {
        [SerializeField] private Transform launchOrigin; // 발사·수집 고정 원점 = Char 본체

        [Header("MCP 자동 검증용 (드래그 입력 시뮬레이션 불가 우회)")]
        [SerializeField] private bool autoLaunchOnPlay;
        [SerializeField] private float autoLaunchDelay = 0.5f;
        [SerializeField] private Vector2 autoLaunchDirection = Vector2.up;

        private static readonly Vector2 OriginFallback = new(0f, -6.70f);

        private BallController _ballController;
        private Vector2 _currentDirection = Vector2.up;

        // 발사·수집 원점(Char 고정). AimController가 조준 계산에 읽는다.
        public Vector2 Origin => launchOrigin ? (Vector2)launchOrigin.position : OriginFallback;
        // 현재 조준/발사 방향. TrajectoryPreview가 궤적을 그리는 데 읽는다(항상 표시).
        public Vector2 CurrentDirection => _currentDirection;

        [Inject]
        public void Construct(BallController ballController) => _ballController = ballController;

        private void Start()
        {
            // 원점을 볼 시스템에 주입: 발사 스폰과 바닥 수집이 같은 Char 원점을 공유한다.
            _ballController?.SetCollectTarget(Origin);
            if (autoLaunchOnPlay) Invoke(nameof(AutoLaunch), autoLaunchDelay);
        }

        private void AutoLaunch() => BeginFiring(autoLaunchDirection);

        // ---- 발사 라이프사이클 (AimController가 호출) ----
        public void BeginFiring(Vector2 direction)
        {
            TrackDirection(direction);
            _ballController?.StartFiring(direction);
        }

        public void SetAimDirection(Vector2 direction)
        {
            TrackDirection(direction);
            _ballController?.SetFireDirection(direction);
        }

        public void StopFiring() => _ballController?.StopFiring();

        private void TrackDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon) _currentDirection = direction.normalized;
        }
    }
}
