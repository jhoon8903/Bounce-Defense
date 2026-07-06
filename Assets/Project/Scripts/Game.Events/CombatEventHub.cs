using System;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Events
{
    // 전투 이벤트 허브 — 실제 구독자가 있는 이벤트만 유지한다(StageController: 웨이브 진행 + 베이스 HP).
    // 새 이벤트는 구독자가 생기는 시점에 함께 추가한다(발화만 있는 이벤트 금지).
    public sealed class CombatEventHub
    {
        public event Action OnKill;
        public event Action<int> OnBreach; // int = 침범한 적의 베이스 피해량
        // 피격 피드백(HitFeedbackController 구독): view=플래시 대상(살상타면 무모델→스킵), pos=숫자 위치(캡처됨), amount, isCrit,
        // hitDir=직격 시 볼 접촉면 법선(움찔 반동 방향 계산용). 2차뎀(번·행뎀·폭발)은 방향 없음 → Vector2.zero(반동 없음).
        public event Action<EnemyView, Vector2, int, bool, Vector2> OnHit;

        public void RaiseKill() => OnKill?.Invoke();
        public void RaiseBreach(int breachDamage) => OnBreach?.Invoke(breachDamage);
        public void RaiseHit(EnemyView view, Vector2 pos, int amount, bool isCrit, Vector2 hitDir) => OnHit?.Invoke(view, pos, amount, isCrit, hitDir);
    }
}
