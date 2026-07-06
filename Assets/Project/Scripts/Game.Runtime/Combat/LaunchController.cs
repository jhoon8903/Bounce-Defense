using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class LaunchController : MonoBehaviour
    {
        [SerializeField] private Transform launchOrigin;

        [Header("MCP 자동 검증용 (드래그 입력 시뮬레이션 불가 우회)")]
        [SerializeField] private bool autoLaunchOnPlay;
        [SerializeField] private float autoLaunchDelay = 0.5f;
        [SerializeField] private Vector2 autoLaunchDirection = Vector2.up;

        private static readonly Vector2 OriginFallback = new(0f, -6.70f);

        private BallController _ballController;
        private Vector2 _currentDirection = Vector2.up;
        
        public Vector2 Origin => launchOrigin ? launchOrigin.position : OriginFallback;
        public Vector2 CurrentDirection => _currentDirection;
        public int ShotsFired => _ballController?.ShotsFired ?? 0;

        [Inject]
        public void Construct(BallController ballController) => _ballController = ballController;

        private void Start()
        {
            _ballController?.SetCollectTarget(Origin);
            if (autoLaunchOnPlay) Invoke(nameof(AutoLaunch), autoLaunchDelay);
        }

        private void AutoLaunch() => BeginFiring(autoLaunchDirection);
        
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

        private void TrackDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > Mathf.Epsilon) _currentDirection = direction.normalized;
        }
    }
}
