using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime.Combat
{
    // 조준 입력 전담: 포인터 → 조준 방향(각도 클램프) → LaunchController에 위임(발사 시작/조준/정지).
    // 캐릭터 비주얼(플립·앵커 회전)과 궤적은 각각 CharView·TrajectoryPreview가 LaunchController.CurrentDirection을 읽어 처리.
    public sealed class AimController : MonoBehaviour
    {
        [SerializeField] private LaunchController launchController;
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [SerializeField] private Camera worldCamera;

        private bool _firingStarted;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
            if (!launchController) launchController = FindObjectOfType<LaunchController>();
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || launchController == null) return;
            if (!pointer.press.isPressed) return;

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

        // +x축 기준 각도를 [minDeg,maxDeg] 상단 아크로 클램프. 아크 밖(아래쪽)은 "가장 가까운 경계"로 붙여
        // 반대편으로 튀는 문제 방지. rawSignedAngle = Vector2.SignedAngle(right, dir) ∈ [-180,180].
        public static float ClampAimAngle(float rawSignedAngle, float minDeg, float maxDeg)
        {
            float a = rawSignedAngle;
            if (a < 0f) a += 360f;                    // [0,360)
            if (a >= minDeg && a <= maxDeg) return a; // 아크 안이면 그대로
            float mid = (minDeg + maxDeg) * 0.5f + 180f; // 금지 아크(아래쪽)의 중점
            return (a > maxDeg && a < mid) ? maxDeg : minDeg;
        }
    }
}
