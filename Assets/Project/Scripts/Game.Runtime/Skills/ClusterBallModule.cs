using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
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
            UnityEngine.Vector2 at = target is Game.Runtime.Enemy.EnemyView ev && ev.Model != null
                ? ev.Model.Position : UnityEngine.Vector2.zero;
            services.SpawnClusterBall(at, _specialDamage);
        }
    }
}
