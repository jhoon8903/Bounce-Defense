using Game.Runtime.Motor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime.Combat
{
    public sealed class AimController : MonoBehaviour
    {
        [SerializeField] private TrajectoryPreview preview;
        [SerializeField] private LaunchController launchController;
        [SerializeField] private Transform launchOrigin;
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [SerializeField] private Camera worldCamera;

        private bool _isAiming;
        private Vector2 _currentDir = Vector2.up;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;
            if (pointer.press.wasPressedThisFrame) _isAiming = true;
            if (_isAiming && pointer.press.isPressed)
            {
                _currentDir = ComputeClampedDirection(pointer.position.ReadValue());
                preview.Draw(launchOrigin.position, _currentDir);
            }
            if (!_isAiming || !pointer.press.wasReleasedThisFrame) return;
            _isAiming = false;
            preview.Hide();
            launchController.Launch(launchOrigin.position, _currentDir);
        }

        private Vector2 ComputeClampedDirection(Vector2 screenPos)
        {
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -worldCamera.transform.position.z));
            Vector2 dir = (Vector2)world - (Vector2)launchOrigin.position;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            float angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.right, dir), minAngleDeg, maxAngleDeg);
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
