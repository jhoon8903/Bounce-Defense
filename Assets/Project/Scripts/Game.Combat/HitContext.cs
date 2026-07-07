using UnityEngine;

namespace Game.Combat
{
    public struct HitContext
    {
        public IDamageable Target;
        public BallSourceType SourceBallType;
        public DamageKind Kind;
        public float BaseDamage;
        public bool CanCrit;
        public bool CanReceiveGlobalModifiers;
        public Vector2 HitNormal;
        public int SourceBallId;
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
