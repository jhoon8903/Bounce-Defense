using Game.Runtime.Combat;
using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    // 스태프(무기) 피벗. CharView가 조준 방향으로 구동한다(AimTo). 스스로 포인터를 읽지 않는다(중복 조준 제거).
    // Char가 뒤집히면(localScale.x=-1) 자식 회전이 미러되므로 로컬 Z를 부호 보정한다.
    public class AnchorView : MonoBehaviour
    {
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [Tooltip("초당 회전 속도(도). 0 이하면 즉시 스냅.")]
        [SerializeField] private float turnSpeedDeg = 720f;

        private float _targetZ;

        private void Awake()
        {
            transform.localRotation = Quaternion.identity;
            _targetZ = 0f;
        }

        private void Update()
        {
            float curZ = transform.localEulerAngles.z;
            float nextZ = turnSpeedDeg <= 0f
                ? _targetZ
                : Mathf.MoveTowardsAngle(curZ, _targetZ, turnSpeedDeg * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, 0f, nextZ);
        }

        // 조준 방향으로 스태프를 향한다. flipped = 부모 Char가 localScale.x=-1로 뒤집혔는지.
        public void AimTo(Vector2 worldDir, bool flipped)
        {
            if (worldDir.sqrMagnitude < 1e-6f) return;
            float worldAngle = AimController.ClampAimAngle(Vector2.SignedAngle(Vector2.right, worldDir), minAngleDeg, maxAngleDeg);
            // 스태프 전방 = 로컬 +Y(Z=0 기준). 미러 시 월드각 = 90-θ → θ = 90-월드각. 정상 시 θ = 월드각-90.
            _targetZ = flipped ? 90f - worldAngle : worldAngle - 90f;
        }
    }
}
