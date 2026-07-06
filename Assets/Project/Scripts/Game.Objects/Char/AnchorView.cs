using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
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
        
        public void SetLocalAngle(float localZ) => transform.localRotation = Quaternion.Euler(0f, 0f, localZ);
    }
}
