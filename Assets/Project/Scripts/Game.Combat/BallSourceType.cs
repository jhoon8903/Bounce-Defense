namespace Game.Combat
{
    public enum BallSourceType
    {
        Normal,
        Fire,
        Ice,
        Laser,
        Ghost,
        Cluster,
        ClusterSpawn, // Cluster가 생성한 특수볼 — 무상속·무재귀, Warm Tin 대상 아님
    }
}
