namespace Game.Runtime.Enemy
{
    // Phase 4(번/냉동) 대비 상태이상 스텁. 지금은 컨테이너만 심어 EnemyModel이 들고 있게 한다.
    public enum EnemyStatusType
    {
        None,
        Burn,   // Fire Ball: 초당 틱 데미지
        Freeze, // Ice Ball: 하강 정지/둔화
    }

    // 적에 걸린 개별 상태이상 1건. Phase 4에서 틱 처리 로직이 붙는다.
    public sealed class StatusInstance
    {
        public EnemyStatusType Type;
        public float Remaining;      // 남은 지속시간(초)
        public int Stacks;           // 중첩 수
        public float TickAccumulator; // 초당 틱 누적기(번 등)

        public StatusInstance(EnemyStatusType type, float duration, int stacks = 1)
        {
            Type = type;
            Remaining = duration;
            Stacks = stacks;
            TickAccumulator = 0f;
        }
    }
}
