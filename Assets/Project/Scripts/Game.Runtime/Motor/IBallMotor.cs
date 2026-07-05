using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public struct BallMotorStepResult
    {
        public int BounceCountThisStep;
        // 이번 스텝에 아레나 '바닥면' 반사가 한 번이라도 있었나(순서 무관, sticky).
        // 단일 "마지막 접촉" 필드로 판정하면 코너 다중 바운스에서 오판(볼 소실/미수집)하므로 손실 없는 신호로 유지.
        public bool HitFloor;
    }

    public interface IBallMotor
    {
        Vector2 Position { get; }
        // 직전 Step에서 맞은 적/블록 콜라이더 목록(콜라이더 기준 dedup, 1접촉=1히트). 벽은 포함하지 않음.
        // 모터가 소유·재사용하는 버퍼라 다음 Step 호출 전에 소비해야 한다(할당 없음).
        IReadOnlyList<Collider2D> LastStepHits { get; }
        void Init(Vector2 position, Vector2 direction, float speed, float radius, LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);
        BallMotorStepResult Step(float deltaTime);
    }
}
