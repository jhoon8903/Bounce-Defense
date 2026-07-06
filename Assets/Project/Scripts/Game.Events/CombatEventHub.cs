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
        public event Action<EnemyView, Vector2, int, bool, Vector2, BallSourceType, DamageKind> OnHit;
        // OnExplosion: Last Match 반경 폭발 연출용. center=킬 위치, radius=폭발 반경(파티클 스케일 산출). CombatVfxController가 1회 스폰.
        public event Action<Vector2, float> OnExplosion;
        // OnLaserRow: Laser 행뎀 연출용. center=행 월드좌표(x=0 중심, y=행 Y). CombatVfxController가 가로 빔 1회 스폰(폭 고정 9칸).
        public event Action<Vector2> OnLaserRow;
        // OnClusterBurst: Cluster 분열 트리거 연출용. center=분열 위치. CombatVfxController가 수류탄 폭발 1회 스폰.
        public event Action<Vector2> OnClusterBurst;
        // OnEnemyDeath: 적 사망 연출용(위치 실림 — OnKill은 위치 없음). pos=사망 위치. CombatVfxController가 돌 깨짐 1회 스폰. 모든 킬(직격·번·행뎀·분열·폭발)에서 발화.
        public event Action<Vector2> OnEnemyDeath;
        // OnBaseHit: 방어선 침범이 캐릭터를 때릴 때(스펙 #3) 피 연출용. pos=캐릭터 위치. CombatVfxController가 피 파티클 1회(config 미배선=무연출).
        public event Action<Vector2> OnBaseHit;

        public void RaiseKill() => OnKill?.Invoke();
        public void RaiseBreach(int breachDamage) => OnBreach?.Invoke(breachDamage);
        public void RaiseHit(EnemyView view, Vector2 pos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType, DamageKind kind) => OnHit?.Invoke(view, pos, amount, isCrit, hitDir, sourceType, kind);
        public void RaiseExplosion(Vector2 center, float radius) => OnExplosion?.Invoke(center, radius);
        public void RaiseLaserRow(Vector2 center) => OnLaserRow?.Invoke(center);
        public void RaiseClusterBurst(Vector2 center) => OnClusterBurst?.Invoke(center);
        public void RaiseEnemyDeath(Vector2 pos) => OnEnemyDeath?.Invoke(pos);
        public void RaiseBaseHit(Vector2 pos) => OnBaseHit?.Invoke(pos);
    }
}
