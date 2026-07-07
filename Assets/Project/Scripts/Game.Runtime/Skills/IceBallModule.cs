using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public sealed class IceBallModule : IBallModule
    {
        private readonly float _chance;
        private readonly float _duration;
        private readonly float _slow;

        public IceBallModule(SkillDefinition skill, int level)
        {
            _chance = skill != null ? skill.GetFreezeChance(level) : 0f;
            _duration = skill != null ? skill.GetFreezeDuration(level) : 0f;
            _slow = skill != null ? skill.GetFreezeSlow(level) : 0f;
        }

        public void OnEnemyHit(IDamageable target, HitContext ctx, IBallEffectContext services)
        {
            if (_chance <= 0f || _duration <= 0f || _slow <= 0f) return;
            if (services?.Random == null) return;
            if (!services.Random.NextBool(_chance)) return;
            if (target is IStatusReceiver receiver) receiver.ApplyFreeze(_duration, _slow);
        }
    }
}
