using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Game.Objects.Char
{
    public class AnchorView : MonoBehaviour
    {
        [Tooltip("켜면 이 컴포넌트가 터치(누르는 중)에 반응해 스스로 조준한다. AimController 가 몰 땐 끈다.")]
        [SerializeField] private bool aimByTouch = true;
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [Tooltip("초당 회전 속도(도). 0 이하면 즉시 스냅.")]
        [SerializeField] private float turnSpeedDeg = 720f;
        [SerializeField] private Camera worldCamera;

        private float _targetZ;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
            transform.localRotation = Quaternion.identity;
            _targetZ = 0f;
        }

        private void Update()
        {
            if (aimByTouch)
            {
                Pointer pointer = Pointer.current;
                if (pointer != null && pointer.press.isPressed && worldCamera)
                {
                    Vector2 screen = pointer.position.ReadValue();
                    Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
                    LookAt((Vector2)world - (Vector2)transform.position);
                }
            }

            float curZ = transform.localEulerAngles.z;
            float nextZ = turnSpeedDeg <= 0f
                ? _targetZ
                : Mathf.MoveTowardsAngle(curZ, _targetZ, turnSpeedDeg * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, 0f, nextZ);
        }
        
        public void LookAt(Vector2 worldDir)
        {
            if (worldDir.sqrMagnitude < 1e-6f) return;
            _targetZ = ComputeAimZ(worldDir, minAngleDeg, maxAngleDeg);
        }
        
        public static float ComputeAimZ(Vector2 worldDir, float minDeg, float maxDeg)
        {
            float fromRight = Mathf.Clamp(Vector2.SignedAngle(Vector2.right, worldDir), minDeg, maxDeg);
            return fromRight - 90f;
        }
    }
}
