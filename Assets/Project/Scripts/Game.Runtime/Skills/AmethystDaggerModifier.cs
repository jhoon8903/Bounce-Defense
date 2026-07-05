using Game.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    // Amethyst Dagger 패시브(플랜 §210·§70): 전면 히트에만 크리 확률 +10/20/30%. 직선 상승 샷 보상.
    // 전면 = HitNormal.y < 0(아래=플레이어 대면, §542). 크리는 직격만 발동(리졸버 CanCrit 게이트) — AppliesTo는 전면 판정만.
    // Emerald(후면)와 자연스레 상호배타: 한 히트의 노멀은 전면/후면 중 하나(§251 최대 40%).
    public sealed class AmethystDaggerModifier : IDamageModifier
    {
        private static readonly float[] CritChance = { 0.10f, 0.20f, 0.30f };

        private readonly float _critChance;

        public AmethystDaggerModifier(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            _critChance = CritChance[i];
        }

        public bool AppliesTo(HitContext context) => context.HitNormal.y < 0f; // 전면(아래)
        public float AdditivePercent => 0f;
        public float CritChanceBonus => _critChance;
    }
}
