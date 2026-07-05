namespace Game.Combat
{
    // 볼의 출처 타입. 데미지 모디파이어 게이팅(예: Warm Tin = Normal만)과 볼별 모듈 배정의 키.
    // 액티브 스킬 5종 + 기본 노멀. (Cluster가 생성한 특수볼은 Phase 4 확장에서 ClusterSpawn 추가.)
    public enum BallSourceType
    {
        Normal,
        Fire,
        Ice,
        Laser,
        Ghost,
        Cluster,
    }
}
