using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public struct BallMotorStepResult
    {
        public int BounceCountThisStep;
        // 이번 스텝의 아레나 '벽면' 반사 횟수(좌·우·천장·바닥). Magic Mirror 무장 신호 — 적 반사와 구분(적은 BounceCount에만).
        public int WallBounceCountThisStep;
        // 이번 스텝에 아레나 '바닥면' 반사가 한 번이라도 있었나(순서 무관, sticky).
        // 단일 "마지막 접촉" 필드로 판정하면 코너 다중 바운스에서 오판(볼 소실/미수집)하므로 손실 없는 신호로 유지.
        public bool HitFloor;
    }

    // 한 스텝의 데미지 접촉 1건 = 콜라이더 + 접촉면 노멀(전/후면 크리 판정용).
    // 반사 히트는 hit.normal, 관통(Ghost) 히트는 진입방향(-dir)으로 세팅(§542: 전면 y<0 / 후면 y>0).
    public readonly struct BallHit
    {
        public readonly Collider2D Collider;
        public readonly Vector2 Normal;
        public readonly Vector2 Point; // 접촉점(레이캐스트 hit.point). 콜라이더 중앙이 아니라 실제 타격 지점 — 임팩트/숫자 위치용.
        public BallHit(Collider2D collider, Vector2 normal, Vector2 point) { Collider = collider; Normal = normal; Point = point; }
    }

    public interface IBallMotor
    {
        Vector2 Position { get; }
        // 직전 Step에서 맞은 적/블록 접촉 목록(콜라이더 기준 dedup, 1접촉=1히트, 접촉면 노멀 동반). 벽은 포함하지 않음.
        // 모터가 소유·재사용하는 버퍼라 다음 Step 호출 전에 소비해야 한다(할당 없음).
        IReadOnlyList<BallHit> LastStepHits { get; }
        void Init(Vector2 position, Vector2 direction, float speed, float radius, LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask);
        BallMotorStepResult Step(float deltaTime);
    }
}
