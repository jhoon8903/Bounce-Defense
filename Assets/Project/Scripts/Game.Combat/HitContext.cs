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
        // 이 히트를 만든 볼의 인스턴스 id(라이브 볼 식별용). Magic Mirror 등 "같은 볼" 판정 스킬용. 미지정 시 0.
        // 전/후면 판정은 별도 필드 없이 HitNormal.y 부호로 한다(위에서 아래로 맞으면 normal.y<0 등).
        public int SourceBallInstanceId { get; }
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
            int chainDepth = 0,
            int sourceBallInstanceId = 0)
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
            SourceBallInstanceId = sourceBallInstanceId;
        }
    }
}
