using System;

namespace Game.Events
{
    // 전투 이벤트 허브 — 실제 구독자가 있는 이벤트만 유지한다(StageController: 웨이브 진행 + 베이스 HP).
    // 새 이벤트는 구독자가 생기는 시점에 함께 추가한다(발화만 있는 이벤트 금지).
    public sealed class CombatEventHub
    {
        public event Action OnKill;
        public event Action<int> OnBreach; // int = 침범한 적의 베이스 피해량

        public void RaiseKill() => OnKill?.Invoke();
        public void RaiseBreach(int breachDamage) => OnBreach?.Invoke(breachDamage);
    }
}
