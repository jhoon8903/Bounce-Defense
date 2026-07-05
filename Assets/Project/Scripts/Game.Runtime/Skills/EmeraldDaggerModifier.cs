using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    // Emerald Dagger 패시브(플랜 §211·§71): 후면 히트에만 크리 확률 +20/30/40%. 위 벽에 튕겨 내려꽂는 샷 보상
    // (더 어려우니 수치도 큼). 후면 = HitNormal.y > 0(위=게이트 대면, §542). 크리 직격만(리졸버 CanCrit 게이트).
    // Amethyst(전면)와 자연스레 상호배타(§251 최대 40%).
    public sealed class EmeraldDaggerModifier : IDamageModifier
    {
        private static readonly float[] CritChance = { 0.20f, 0.30f, 0.40f };

        private readonly float _critChance;

        public EmeraldDaggerModifier(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            _critChance = CritChance[i];
        }

        public bool AppliesTo(HitContext context) => context.HitNormal.y > 0f; // 후면(위)
        public float AdditivePercent => 0f;
        public float CritChanceBonus => _critChance;
    }
}
