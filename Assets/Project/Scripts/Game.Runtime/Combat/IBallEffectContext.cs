using Game.Combat;
using Game.Core.Random;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 모듈이 온-히트에 코어 밖 서비스로 손을 뻗는 유일한 창구(Option B, §11-17 결정).
    // 오너(BallController=팩토리·모터·리졸버 소유, GridController=그리드 소유) 위의 얇은 파사드:
    // 모듈은 고수준 op만 부르고, 그리드/팩토리로 실제 손 뻗는 배관은 이 구현 한 곳에만 산다
    // (모듈이 여러 서브시스템을 넘나드는 비용 제거). BallController가 구현해 자신을 모듈에 넘긴다.
    //   각 op는 필요한 스킬이 붙을 때 추가된다(발화만 있는 API 금지, CombatEventHub 규율과 동일):
    //   Laser → DamageEnemyRow, Cluster → SpawnClusterBall (fan-out에서 배선).
    public interface IBallEffectContext
    {
        // 시드 RNG(§265 결정론). Ice 냉동 롤·Cluster 분열 롤 등 확률 판정에 사용.
        IRandom Random { get; }

        // Laser: originEnemy와 같은 그리드 행의 '다른' 적들에게 flat 2차뎀(무크리·무버프, 열 오름차순 결정론·블록 중복 dedup).
        //   AoE 처치도 동일 리졸버 경유라 RaiseKill 발동 → Last Match 등 킬 이벤트가 그대로 이어짐(§255).
        void DamageEnemyRow(IDamageable originEnemy, float flatDamage, BallSourceType source);

        // Cluster: 지정 위치서 분열 특수볼 1개 스폰(무작위 상향·2차뎀·무재귀·로스터 미집계).
        void SpawnClusterBall(Vector2 origin, float damage);
    }
}
