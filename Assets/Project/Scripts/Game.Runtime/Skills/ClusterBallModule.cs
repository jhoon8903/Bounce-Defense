using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // Cluster Ball 볼 모듈: 직격 시 확률로 히트 위치서 분열 특수볼 1개 스폰(§202). 특수볼 = 2차뎀·무재귀·무상속.
    // 스폰·회계는 services.SpawnClusterBall이 처리(모듈은 롤+위치만). 스폰 롤 = 시드 RNG(결정론).
    public sealed class ClusterBallModule : IBallModule
    {
        private readonly float _chance;
        private readonly float _specialDamage;

        public ClusterBallModule(SkillDefinition skill, int level)
        {
            _chance = skill != null ? skill.GetClusterChance(level) : 0f;
            _specialDamage = skill != null ? skill.GetClusterSpecialDamage(level) : 0f;
        }

        public void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services)
        {
            if (_chance <= 0f || _specialDamage <= 0f || services?.Random == null) return;
            if (!services.Random.NextBool(_chance)) return;
            // 히트 적 위치서 분열(적이 곧 디스폰돼도 위치값은 유효).
            UnityEngine.Vector2 at = target is Game.Runtime.Enemy.EnemyView ev && ev.Model != null
                ? ev.Model.Position : UnityEngine.Vector2.zero;
            services.SpawnClusterBall(at, _specialDamage);
        }
    }
}
