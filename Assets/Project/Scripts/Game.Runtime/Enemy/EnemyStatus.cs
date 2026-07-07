namespace Game.Runtime.Enemy
{
    public enum EnemyStatusType
    {
        None,
        Burn,
        Freeze,
    }

    public sealed class StatusInstance
    {
        public EnemyStatusType Type;
        public float Remaining;
        public float Dps;
        public float Slow;
        public float TickAccumulator;

        public StatusInstance(EnemyStatusType type, float duration, float dps)
        {
            Type = type;
            Remaining = duration;
            Dps = dps;
            TickAccumulator = 0f;
        }
    }
}
