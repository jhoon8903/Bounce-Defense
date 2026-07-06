using System;
using Game.Combat;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Events
{
    public sealed class CombatEventHub
    {
        public event Action OnKill;
        public event Action<int> OnBreach;
        // OnHit: view=플래시/움찔 대상, pos=숫자 위치(캡처됨), amount, isCrit, hitDir(직격=법선·2차뎀=zero), sourceType=볼 타입(임팩트 프리팹 선택).
        public event Action<EnemyView, Vector2, int, bool, Vector2, BallSourceType> OnHit;

        public void RaiseKill() => OnKill?.Invoke();
        public void RaiseBreach(int breachDamage) => OnBreach?.Invoke(breachDamage);
        public void RaiseHit(EnemyView view, Vector2 pos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType) => OnHit?.Invoke(view, pos, amount, isCrit, hitDir, sourceType);
    }
}
