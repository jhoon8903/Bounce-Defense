using Game.Combat;
using Game.Runtime.Combat;
using UnityEngine;

namespace Game.Runtime.Skills
{
    // Fire Ball 볼 모듈: 직격 시 대상에 번(초당 틱) 부여. 번 데미지 자체는 EnemyStatusSimulator가 틱(flat·무크리·무버프).
    // 레벨별(플랜 §198): 지속 4/4.5/5s · dps 8/10/12 · 최대중첩 3/4/5.
    public sealed class FireBallModule : IBallModule
    {
        private static readonly float[] Duration = { 4f, 4.5f, 5f };
        private static readonly float[] Dps = { 8f, 10f, 12f };
        private static readonly int[] MaxStacks = { 3, 4, 5 };

        private readonly float _duration;
        private readonly float _dps;
        private readonly int _maxStacks;

        public FireBallModule(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, 2);
            _duration = Duration[i];
            _dps = Dps[i];
            _maxStacks = MaxStacks[i];
        }

        public void OnEnemyHit(IDamageable target, HitContext ctx)
        {
            if (target is IStatusReceiver receiver)
                receiver.ApplyBurn(_duration, _dps, _maxStacks);
        }
    }
}
