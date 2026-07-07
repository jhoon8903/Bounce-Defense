using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
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
            if (target is IStatusReceiver receiver) receiver.ApplyBurn(_duration, _dps, _maxStacks);
        }
    }
}
