using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // Fire Ball 볼 모듈: 직격 시 대상에 번(초당 틱) 부여. 번 데미지 자체는 EnemyStatusSimulator가 틱(flat·무크리·무버프).
    // 레벨별 수치(지속·dps·최대중첩, 플랜 §198)는 SkillDefinition(§11-17 결정: 인스펙터 튜닝)에서 읽는다.
    public sealed class FireBallModule : IBallModule
    {
        private readonly float _duration;
        private readonly float _dps;
        private readonly int _maxStacks;

        public FireBallModule(SkillDefinition skill, int level)
        {
            _duration = skill != null ? skill.GetBurnDuration(level) : 0f;
            _dps = skill != null ? skill.GetBurnDps(level) : 0f;
            _maxStacks = skill != null ? skill.GetBurnMaxStacks(level) : 0;
        }

        public void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services)
        {
            if (_dps <= 0f || _duration <= 0f) return;
            if (target is IStatusReceiver receiver)
                receiver.ApplyBurn(_duration, _dps, _maxStacks);
        }
    }
}
