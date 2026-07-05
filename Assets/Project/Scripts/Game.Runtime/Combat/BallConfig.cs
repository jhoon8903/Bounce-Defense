using Game.Combat;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 타입별 순수 view/motion/pool config(프리팹=PoolConfiguration 기반). 데미지 수치는 여기 두지 않는다:
    //  - 액티브 볼 뎀 = SkillDefinition.ballDamagePerLevel(§11-17), 노멀 볼 뎀 = SkillRuntime.NormalBallDamage 상수.
    //  - (구 damagePerLevel 필드는 액티브 config에선 안 읽히던 죽은 필드라 제거 — 노멀만 읽던 것도 SkillRuntime으로 이전.)
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Game/Configs/BallConfig")]
    public sealed class BallConfig : PoolConfiguration
    {
        [Header("Type")]
        // 이 config(=타입별 프리팹)의 볼 타입. BallFactory가 타입별 풀 키로 사용(§11-9).
        [SerializeField] private BallSourceType sourceType = BallSourceType.Normal;

        [Header("Motion")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float radius = 0.15f;

        public BallSourceType SourceType => sourceType;
        public float Speed => speed;
        public float Radius => radius;
    }
}
