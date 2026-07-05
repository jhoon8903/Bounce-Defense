namespace Game.Combat
{
    // 데미지 파이프라인 확장점(개방-폐쇄). 스킬 패시브가 self-gating 모디파이어를 ModifierRegistry에 등록만 하고
    // DamageResolver·코어 루프는 무수정. AppliesTo가 이 히트에 적용 여부를 스스로 판정한다.
    public interface IDamageModifier
    {
        bool AppliesTo(HitContext context);
        float AdditivePercent { get; } // 가산 버킷(0.20 = +20%). 합산 후 1회 곱.
        float CritChanceBonus { get; } // 크리 확률 가산(0.10 = +10%p).
    }
}
