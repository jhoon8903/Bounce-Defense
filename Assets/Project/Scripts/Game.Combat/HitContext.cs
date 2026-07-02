using UnityEngine;

namespace Game.Combat
{
    public sealed class HitContext
    {
        public DamageType DamageType { get; }
        public BallSourceType SourceBallType { get; }
        public IDamageable Target { get; }
        public float BaseDamage { get; }
        public bool CanCrit { get; }
        public bool CanReceiveGlobalModifiers { get; }
        public int ChainDepth { get; }
        public Vector2 HitPosition { get; }
        public Vector2 HitNormal { get; }
        public float WorkingValue { get; set; }
        public float AdditivePercentSum { get; set; }
        public bool IsCrit { get; set; }
        public int FinalDamage { get; set; }

        public HitContext(
            DamageType damageType,
            BallSourceType sourceBallType,
            IDamageable target,
            float baseDamage,
            bool canCrit,
            bool canReceiveGlobalModifiers,
            Vector2 hitPosition = default,
            Vector2 hitNormal = default,
            int chainDepth = 0)
        {
            DamageType = damageType;
            SourceBallType = sourceBallType;
            Target = target;
            BaseDamage = baseDamage;
            CanCrit = canCrit;
            CanReceiveGlobalModifiers = canReceiveGlobalModifiers;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            ChainDepth = chainDepth;
        }
    }
}
