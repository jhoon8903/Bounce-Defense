using Game.Combat;
using Game.Core.Pool;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 타입별 임팩트 파티클 config(BallConfig 미러). 프리팹=PoolConfiguration 기반(풀명·용량 상속).
    //  - CombatVfxController가 타입별 Pool<ImpactVfxView>를 이 config로 소유(§11-9 BallFactory 패턴과 동일).
    //  - GamePool은 View 타입으로 풀을 나눠 6개 임팩트 프리팹이 한 풀로 충돌 → 여기서 config별로 분리(Game.Core 풀 무수정).
    [CreateAssetMenu(fileName = "ImpactConfig", menuName = "Game/Configs/ImpactConfig")]
    public sealed class ImpactConfig : PoolConfiguration
    {
        [Header("Type")]
        // 이 config(=타입별 임팩트 프리팹)의 볼 타입. CombatVfxController가 타입별 풀 키로 사용.
        [SerializeField] private BallSourceType sourceType = BallSourceType.Normal;

        public BallSourceType SourceType => sourceType;
    }
}
