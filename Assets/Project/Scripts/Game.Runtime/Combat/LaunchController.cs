using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class LaunchController : MonoBehaviour
    {
        [SerializeField] private Transform launchOrigin;

#if UNITY_EDITOR
        [Header("에디터 전용: MCP 자동 검증 + 프로파일링용 자동 스윕 발사")]
        [SerializeField] private bool autoLaunchOnPlay;
        [SerializeField] private float autoLaunchDelay = 0.5f;
        [SerializeField] private Vector2 autoLaunchDirection = Vector2.up;
        [SerializeField] private bool autoSweep = true;
        [SerializeField] private float autoSweepDegrees = 55f;
        [SerializeField] private float autoSweepSpeed = 2f;
        private bool _autoSweeping;
#endif

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
#if UNITY_EDITOR
            if (autoLaunchOnPlay) Invoke(nameof(AutoLaunch), autoLaunchDelay);
#endif
        }

#if UNITY_EDITOR
        private void AutoLaunch()
        {
            BeginFiring(autoLaunchDirection);
            _autoSweeping = autoSweep;
        }

        private void Update()
        {
            if (!_autoSweeping) return;
            float ang = (90f + autoSweepDegrees * Mathf.Sin(Time.time * autoSweepSpeed)) * Mathf.Deg2Rad;
            SetAimDirection(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)));
        }
#endif

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
