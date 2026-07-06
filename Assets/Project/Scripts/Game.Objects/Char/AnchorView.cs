using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    // 스태프(무기) 피벗 = dumb 렌더러. 스스로 시간을 돌리지 않는다(자체 Update 제거).
    // CharController(IClock 틱)가 조준 스무딩·반동을 GameDeltaTime으로 계산해 SetLocalAngle로 밀어넣는다.
    // 튜닝 값(각도 한계·회전속도·반동)만 인스펙터에 소유하고 컨트롤러가 읽어간다.
    public class AnchorView : MonoBehaviour
    {
        [SerializeField] private float minAngleDeg = 15f;
        [SerializeField] private float maxAngleDeg = 165f;
        [Tooltip("초당 회전 속도(도). 0 이하면 즉시 스냅.")]
        [SerializeField] private float turnSpeedDeg = 720f;
        [Tooltip("발사 반동 킥 크기(도). 총 쏘듯 까딱(§8).")]
        [SerializeField] private float recoilKickDeg = 10f;
        [Tooltip("반동 원복 속도(도/초).")]
        [SerializeField] private float recoilReturnDeg = 140f;

        public float MinAngleDeg => minAngleDeg;
        public float MaxAngleDeg => maxAngleDeg;
        public float TurnSpeedDeg => turnSpeedDeg;
        public float RecoilKickDeg => recoilKickDeg;
        public float RecoilReturnDeg => recoilReturnDeg;

        private void Awake() => transform.localRotation = Quaternion.identity;

        // 컨트롤러가 매 틱 계산한 최종 로컬 Z를 그대로 적용(조준 스무딩 + 반동 합성 결과).
        public void SetLocalAngle(float localZ) => transform.localRotation = Quaternion.Euler(0f, 0f, localZ);
    }
}
