namespace Game.Combat
{
    // 상태이상(번·냉동)을 받을 수 있는 대상. EnemyView가 구현해 컨트롤러/상태 시뮬레이터로 포워드.
    // 볼 모듈(FireBall/IceBall)이 히트 대상(IDamageable)을 이 인터페이스로 캐스팅해 상태를 부여한다.
    public interface IStatusReceiver
    {
        // 번(초당 틱) 부여. 독립타이머 스택(캡까지), flat·무크리·무버프.
        void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks);

        // 냉동 부여(Ice). 지속 동안 하강 슬로우(0.20=20% 감속). 무스택(refresh, 가장 강한 슬로우·최장 지속 유지).
        void ApplyFreeze(float durationSeconds, float slow);
    }
}
