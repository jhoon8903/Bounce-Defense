using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public struct BallMotorStepResult
    {
        public bool HitWall;
        public bool HitEnemy;
        public bool HitBlock;
        public bool PassedThrough;
        public Vector2 HitPoint;
        public Vector2 HitNormal;
        public Collider2D HitCollider;
        public int BounceCountThisStep;
        public bool StuckAborted;
        // 아래 3종은 단일 result 필드가 다중 바운스 스텝(코너)에 덮이는 문제를 피하기 위한 손실 없는 신호.
        // HitWall/HitNormal은 "마지막" 접촉만 담으므로 바닥 수집 판정·벽 이벤트에 쓰면 오판(코너에서 볼 소실/미수집)한다.
        public bool HitFloor;         // 이번 스텝에 아레나 '바닥면' 반사가 한 번이라도 있었나(순서 무관, sticky).
        public bool HitNonFloorWall;  // 이번 스텝에 아레나 '옆/천장면' 반사가 한 번이라도 있었나(벽 이벤트용, sticky).
        public Vector2 WallBounceNormal; // 비바닥 벽 반사의 대표 노멀(이벤트용).
        public Vector2 WallBouncePoint;  // 비바닥 벽 반사의 대표 지점(이벤트용).
    }

    // 한 Step에서 발생한 데미지 대상(적/블록) 접촉 1건. 단일 result 필드는 벽 반사에 덮여 손실될 수 있으므로
    // 데미지 배선은 이 개별 히트 목록(LastStepHits)을 쓴다. Normal은 반사 전 표면 노멀(전/후면 판정용).
    public readonly struct MotorHit
    {
        public readonly Collider2D Collider;
        public readonly Vector2 Point;
        public readonly Vector2 Normal;
        public readonly bool IsBlock; // true=Block 레이어, false=Enemy 레이어

        public MotorHit(Collider2D collider, Vector2 point, Vector2 normal, bool isBlock)
        {
            Collider = collider;
            Point = point;
            Normal = normal;
            IsBlock = isBlock;
        }
    }

    public interface IBallMotor
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        float Speed { get; }
        // 직전 Step에서 맞은 적/블록 접촉 목록(콜라이더 기준 dedup, 1접촉=1히트). 벽은 포함하지 않음.
        // 모터가 소유·재사용하는 버퍼라 다음 Step 호출 전에 소비해야 한다(할당 없음).
        IReadOnlyList<MotorHit> LastStepHits { get; }
        void Init(Vector2 position, Vector2 direction, float speed, float radius, LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);
        BallMotorStepResult Step(float deltaTime);
    }
}
