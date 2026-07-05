using UnityEngine;

namespace Game.Combat
{
    // 데미지 규칙의 단일 소유자: 원본 float 데미지 → 최종 int 적용을 여기서만 결정한다.
    // 단일 반올림 원칙: 호출부는 미리 반올림하지 않고 원본 float를 넘긴다(파이프라인 끝에서 한 번만).
    // 배수/크리/상태이상 등 규칙이 생기면(카드·스킬) 이 클래스가 확장 지점이다.
    public sealed class DamageResolver
    {
        public void Resolve(IDamageable target, float baseDamage)
        {
            if (target == null) return;
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage));
            target.ApplyDamage(finalDamage);
        }
    }
}
