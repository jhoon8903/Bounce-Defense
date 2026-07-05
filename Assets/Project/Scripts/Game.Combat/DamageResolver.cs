using Game.Core.Random;
using UnityEngine;

namespace Game.Combat
{
    // 데미지 규칙의 단일 소유자. 단일 반올림(최종만) 원칙 유지.
    //   스테이지: Base → Additive% → CritChance(roll) → CritDamage(×1.5) → Round(최종만) → Apply.
    //   final = round( base × (1 + Σadditive%) × (isCrit ? 1.5 : 1) ), clamp ≥ 0   (플랜 §174)
    // 확장은 ModifierRegistry의 self-gating IDamageModifier로만 — resolver·코어 루프 무수정(개방-폐쇄).
    public sealed class DamageResolver
    {
        private const float CritMultiplier = 1.5f; // 스펙: 크리 데미지 정확히 ×1.5(올리는 스킬 없음).

        private readonly ModifierRegistry _modifiers;
        private readonly IRandom _random;

        public DamageResolver(ModifierRegistry modifiers, IRandom random)
        {
            _modifiers = modifiers;
            _random = random;
        }

        // 원본 float 데미지 → 파이프라인 → 최종 int 적용. ctx.IsCrit/FinalDamage를 채워 호출부(볼 모듈)가 읽는다.
        public void Resolve(HitContext ctx)
        {
            if (ctx == null || ctx.Target == null) return;

            float working = ctx.BaseDamage;

            // 가산 버킷 = 레지스트리 self-gating 모디파이어 합 + 이 히트 소비형 보너스(Magic Mirror). 단일 곱(§185).
            if (ctx.CanReceiveGlobalModifiers)
            {
                float additive = _modifiers != null ? _modifiers.GetAdditivePercentSum(ctx) : 0f;
                additive += ctx.BonusAdditivePercent;
                working *= 1f + additive;
            }

            if (ctx.CanCrit && _modifiers != null && _random != null)
            {
                ctx.IsCrit = _random.NextBool(_modifiers.GetCritChance(ctx));
                if (ctx.IsCrit) working *= CritMultiplier;
            }

            ctx.FinalDamage = Mathf.Max(0, Mathf.RoundToInt(working));
            ctx.Target.ApplyDamage(ctx.FinalDamage);
        }

        // 그리드 브리지/레거시 경로용 얇은 오버로드(모디파이어·크리 없이 단일 반올림만).
        public void Resolve(IDamageable target, float baseDamage)
        {
            if (target == null) return;
            target.ApplyDamage(Mathf.Max(0, Mathf.RoundToInt(baseDamage)));
        }
    }
}
