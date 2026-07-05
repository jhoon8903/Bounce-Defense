namespace Game.Skills
{
    // 스킬의 런타임 동작 식별자(데이터). Game.Skills asmdef는 Game.Combat(Assembly-CSharp)을 참조할 수 없으므로
    // 전투 개념(BallSourceType/모듈)은 여기 두지 않는다 — Assembly-CSharp의 SkillModuleFactory가 이 값을 번역한다.
    // 11번째 스킬 = 여기 값 1개 + 모듈 클래스 1개 + 팩토리 case 1줄(코어 루프·리졸버 무수정).
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
    }
}
