using UnityEngine;

namespace Game.Combat
{
    // 한 번의 데미지 적용에 대한 가변 컨텍스트. 리졸버가 IsCrit/FinalDamage를 채워 넣어 호출부(볼 모듈)가 읽는다.
    //  - 직격(Direct) = 크리·글로벌 모디파이어 대상. 팩토리 Direct(...) 사용.
    //  - 2차원(Burn/LaserRow/Explosion/ClusterSpawn) = 자체 flat, 크리·모디파이어 제외(더블디핑 방지). 팩토리 Secondary(...).
    public sealed class HitContext
    {
        public IDamageable Target;
        public BallSourceType SourceBallType;
        public DamageKind Kind;
        public float BaseDamage;
        public bool CanCrit;
        public bool CanReceiveGlobalModifiers;
        public Vector2 HitNormal; // 전/후면 크리 판정용(단검 패시브). 미사용 시 default.
        public int SourceBallId;  // "같은 볼" 판정(Magic Mirror 등). 미지정 0.

        // 리졸버가 채우는 작업 필드.
        public bool IsCrit;
        public int FinalDamage;

        public static HitContext Direct(IDamageable target, BallSourceType sourceType, float baseDamage,
            Vector2 hitNormal = default, int sourceBallId = 0)
        {
            return new HitContext
            {
                Target = target,
                SourceBallType = sourceType,
                Kind = DamageKind.Direct,
                BaseDamage = baseDamage,
                CanCrit = true,
                CanReceiveGlobalModifiers = true,
                HitNormal = hitNormal,
                SourceBallId = sourceBallId,
            };
        }

        public static HitContext Secondary(IDamageable target, BallSourceType sourceType, DamageKind kind, float baseDamage)
        {
            return new HitContext
            {
                Target = target,
                SourceBallType = sourceType,
                Kind = kind,
                BaseDamage = baseDamage,
                CanCrit = false,
                CanReceiveGlobalModifiers = false,
            };
        }
    }
}
