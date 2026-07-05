namespace Game.Runtime.Enemy
{
    // 상태이상 종류. Burn = Fire 초당 틱. Freeze = Ice 하강 둔화/정지(fan-out).
    public enum EnemyStatusType
    {
        None,
        Burn,
        Freeze,
    }

    // 적에 걸린 개별 상태이상 1건(독립 타이머). Burn = 초당 Dps flat. Freeze = Slow(하강 감속률).
    public sealed class StatusInstance
    {
        public EnemyStatusType Type;
        public float Remaining;       // 남은 지속시간(초)
        public float Dps;             // 초당 데미지(번)
        public float Slow;            // 하강 감속률(냉동, 0.20 = 20%)
        public float TickAccumulator; // 초당 틱 누적기

        public StatusInstance(EnemyStatusType type, float duration, float dps)
        {
            Type = type;
            Remaining = duration;
            Dps = dps;
            TickAccumulator = 0f;
        }
    }
}
