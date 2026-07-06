namespace Game.Skills
{
    public enum SkillEffectKind
    {
        None = 0,
        // 액티브(볼) 5
        FireBall,
        IceBall,
        LaserBall,
        GhostBall,
        ClusterBall,
        // 패시브 5
        WarmTin,
        MagicMirror,
        AmethystDagger,
        EmeraldDagger,
        LastMatch,
        // 노멀(무스킬) 볼 — 결과창 집계 표시용. 반드시 맨 끝(직렬화 값 안정).
        NormalBall,
    }
}
