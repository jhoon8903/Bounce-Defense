using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class LaunchController : MonoBehaviour
    {
        [SerializeField] private int ballCount = 5;
        [SerializeField] private Transform launchOrigin;

        [Header("MCP 자동 검증용 (드래그 입력 시뮬레이션 불가 우회)")]
        [SerializeField] private bool autoLaunchOnPlay;
        [SerializeField] private float autoLaunchDelay = 0.5f;
        [SerializeField] private Vector2 autoLaunchDirection = Vector2.up;

        private BallController _ballController;

        [Inject]
        public void Construct(BallController ballController) => _ballController = ballController;

        private void Start()
        {
            if (autoLaunchOnPlay) Invoke(nameof(AutoLaunch), autoLaunchDelay);
        }

        private void AutoLaunch()
        {
            Vector2 origin = launchOrigin ? (Vector2)launchOrigin.position : Vector2.zero;
            Launch(origin, autoLaunchDirection);
        }

        public void Launch(Vector2 origin, Vector2 direction) => _ballController?.FireVolley(origin, direction, ballCount);
    }
}
