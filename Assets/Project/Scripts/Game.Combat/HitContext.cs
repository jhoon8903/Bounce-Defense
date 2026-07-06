using UnityEngine;

namespace Game.Combat
{
    // struct(값 타입): 매 적중 힙할당 제거(핫패스 GC). DamageResolver.Resolve는 ref로 받아 IsCrit/FinalDamage를 호출부 복사에 기록.
    public struct HitContext
    {
        public IDamageable Target;
        public BallSourceType SourceBallType;
        public DamageKind Kind;
        public float BaseDamage;
        public bool CanCrit;
        public bool CanReceiveGlobalModifiers;
        public Vector2 HitNormal; // 전/후면 크리 판정용(단검 패시브). 미사용 시 default.
        public int SourceBallId;  // "같은 볼" 판정(Magic Mirror 등). 미지정 0.
        public float BonusAdditivePercent;
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
