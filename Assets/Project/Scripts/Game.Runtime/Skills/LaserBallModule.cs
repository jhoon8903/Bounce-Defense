using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public sealed class LaserBallModule : IBallModule
    {
        private readonly float _rowDamage;

        public LaserBallModule(SkillDefinition skill, int level)
        {
            _rowDamage = skill != null ? skill.GetLaserRowDamage(level) : 0f;
        }

        public void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services)
        {
            if (_rowDamage <= 0f || services == null) return;
            services.DamageEnemyRow(target, _rowDamage, BallSourceType.Laser);
        }
    }
}
