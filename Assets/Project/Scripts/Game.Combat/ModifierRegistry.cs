using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    // 등록된 데미지 모디파이어(패시브)의 얇은 컬렉션. 각 모디파이어는 self-gating(AppliesTo)이라
    // 레지스트리는 합산만 한다. SkillRuntime이 로드아웃 변경 시 Register/Clear로 갱신.
    public sealed class ModifierRegistry
    {
        private readonly List<IDamageModifier> _modifiers = new();

        public void Register(IDamageModifier modifier)
        {
            if (modifier == null || _modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }

        public void Unregister(IDamageModifier modifier) => _modifiers.Remove(modifier);

        public void Clear() => _modifiers.Clear();

        public float GetAdditivePercentSum(HitContext context)
        {
            float sum = 0f;
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i].AppliesTo(context)) sum += _modifiers[i].AdditivePercent;
            return sum;
        }

        public float GetCritChance(HitContext context)
        {
            float sum = 0f;
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i].AppliesTo(context)) sum += _modifiers[i].CritChanceBonus;
            return Mathf.Clamp01(sum);
        }
    }
}
