namespace Game.Combat
{
    public enum DamageKind
    {
        Direct,       // 볼-적 직격
        Burn,         // Fire Ball 초당 틱
        LaserRow,     // Laser Ball 같은 행 flat
        Explosion,    // Last Match 폭발
        ClusterSpawn, // Cluster 특수볼
    }
}
