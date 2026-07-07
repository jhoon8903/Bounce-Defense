using Game.Core.Clock;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class AimController : MonoBehaviour
    {
        [SerializeField] private LaunchController launchController;
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [SerializeField] private Camera worldCamera;

        private bool _firingStarted;
        private IClock _clock;

        [Inject]
        public void Construct(IClock clock) => _clock = clock;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
            if (!launchController) launchController = FindFirstObjectByType<LaunchController>();
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || launchController == null) return;
            if (!pointer.press.isPressed) return;
            if (_clock is { GameSpeed: <= 0f } || IsPointerOverUI()) return;
            Vector2 dir = ComputeClampedDirection(pointer.position.ReadValue(), launchController.Origin);
            if (!_firingStarted)
            {
                launchController.BeginFiring(dir);
                _firingStarted = true;
            }
            else
            {
                launchController.SetAimDirection(dir);
            }
        }

        private static bool IsPointerOverUI()
        {
            EventSystem es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        private Vector2 ComputeClampedDirection(Vector2 screenPos, Vector2 origin)
        {
            if (worldCamera == null) return Vector2.up;
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -worldCamera.transform.position.z));
            Vector2 dir = (Vector2)world - origin;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            float angle = ClampAimAngle(Vector2.SignedAngle(Vector2.right, dir), minAngleDeg, maxAngleDeg);
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static float ClampAimAngle(float rawSignedAngle, float minDeg, float maxDeg)
        {
            float a = rawSignedAngle;
            if (a < 0f) a += 360f;
            if (a >= minDeg && a <= maxDeg) return a;
            float mid = (minDeg + maxDeg) * 0.5f + 180f;
            return (a > maxDeg && a < mid) ? maxDeg : minDeg;
        }
    }
}
