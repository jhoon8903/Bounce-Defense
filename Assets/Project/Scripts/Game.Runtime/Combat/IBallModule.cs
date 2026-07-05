using Game.Combat;

namespace Game.Runtime.Combat
{
    // 볼별 온-히트 동작(개방-폐쇄). 스킬은 자기 모듈을 볼에 실을 뿐 BallController·데미지 경로는 무수정.
    // 직격 데미지는 이미 DamageResolver가 적용한 뒤(ctx.FinalDamage 확정) 호출된다 — 모듈은 부가 효과(번 부여 등)만.
    //   services = 코어 밖 서비스(랜덤·행뎀·분열스폰) 파사드. 대상만으로 되는 효과(Fire 번)는 무시하면 된다.
    public interface IBallModule
    {
        void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services);
    }
}
