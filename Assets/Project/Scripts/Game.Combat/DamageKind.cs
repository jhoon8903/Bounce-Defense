namespace Game.Combat
{
    // 데미지 인스턴스의 종류. 직격만 크리·글로벌 모디파이어 대상(플랜 §179·§181).
    // 2차 데미지원(번·행뎀·폭발·클러스터)은 자체 flat 주입이나 동일 DamageResolver를 통과해 숫자표기·사망을 통일.
    public enum DamageKind
    {
        Direct,       // 볼-적 직격
        Burn,         // Fire Ball 초당 틱
        LaserRow,     // Laser Ball 같은 행 flat
        Explosion,    // Last Match 폭발
        ClusterSpawn, // Cluster 특수볼
    }
}
