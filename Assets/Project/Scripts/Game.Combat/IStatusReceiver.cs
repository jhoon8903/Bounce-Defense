namespace Game.Combat
{
    // 상태이상(번 등)을 받을 수 있는 대상. EnemyView가 구현해 컨트롤러/상태 시뮬레이터로 포워드.
    // 볼 모듈(FireBallModule)이 히트 대상(IDamageable)을 이 인터페이스로 캐스팅해 상태를 부여한다.
    public interface IStatusReceiver
    {
        // 번(초당 틱) 부여. 독립타이머 스택(캡까지), flat·무크리·무버프. (Ice 냉동은 fan-out에서 추가.)
        void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks);
    }
}
