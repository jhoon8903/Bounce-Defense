using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // Ice Ball 볼 모듈: 직격 시 확률로 냉동 부여(하강 슬로우·지속). 냉동 자체 데미지 없음(§199·§254).
    // 냉동 롤 = 시드 RNG(services.Random)로 결정론. 상시 추가뎀은 별도 IceBonusModifier(가산 버킷).
    // 레벨별 수치는 SkillDefinition(§11-17 결정: 인스펙터 튜닝)에서 읽는다.
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
