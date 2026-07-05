using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // Laser Ball 볼 모듈: 직격(볼뎀은 이미 리졸버가 적용) 후, 같은 그리드 행의 '다른' 적들에게 flat 행뎀(§200).
    // 행뎀 = 2차 데미지(무크리·무버프). 그리드 행 조회·결정론 순서·블록 dedup은 services.DamageEnemyRow가 처리.
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
