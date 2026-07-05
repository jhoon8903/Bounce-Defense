using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public sealed class KinematicRaycastMotor : IBallMotor
    {
        private const int MaxIterationsPerStep = 8;
        private const float SkinWidth = 0.01f;
        private const float MinRemaining = 0.0001f;
        // 아레나는 축정렬 박스 → 바닥면 반사 노멀은 정확히 (0,+1). 옆면(±1,0)·천장(0,-1)과 이 값으로 명확히 구분된다.
        private const float FloorFaceNormalY = 0.5f;
        private Vector2 _position;
        private Vector2 _velocity;
        private float _speed;
        private float _radius;
        private LayerMask _wallMask;
        private LayerMask _enemyMask;
        private LayerMask _blockMask;
        private LayerMask _passThroughMask;
        private LayerMask _castMask;
        private readonly HashSet<Collider2D> _ignoredThisStep = new();
        // 이번 Step에서 맞은 적/블록 접촉(재사용 버퍼, 할당 없음). Step 시작에 Clear.
        private readonly List<Collider2D> _stepHits = new();

        public Vector2 Position => _position;
        public IReadOnlyList<Collider2D> LastStepHits => _stepHits;

        public void Init(Vector2 position, Vector2 direction, float speed, float radius,
            LayerMask wallMask, LayerMask enemyMask, LayerMask blockMask, LayerMask passThroughMask)
        {
            _position = position;
            _speed = speed;
            _velocity = direction.normalized * speed;
            _radius = radius;
            _wallMask = wallMask;
            _enemyMask = enemyMask;
            _blockMask = blockMask;
            _passThroughMask = passThroughMask;
            _castMask = wallMask | enemyMask | blockMask;
        }

        public BallMotorStepResult Step(float deltaTime)
        {
            BallMotorStepResult result = new BallMotorStepResult();
            float remaining = _speed * deltaTime;
            int iterations = 0;
            _ignoredThisStep.Clear();
            _stepHits.Clear();
            Collider2D arena = _wallMask.value != 0 ? Physics2D.OverlapPoint(_position, _wallMask) : null;
            bool hasArena = arena != null;
            while (remaining > MinRemaining && iterations < MaxIterationsPerStep)
            {
                iterations++;
                Vector2 dir = _velocity.normalized;
                float wallDist = float.PositiveInfinity;
                Vector2 wallNormal = Vector2.zero;
                if (hasArena) wallDist = MotorGeometry.DistanceToInnerWall(_position, dir, arena.bounds, _radius, out wallNormal);
                RaycastHit2D? chosen = null;
                if (_castMask.value != 0)
                {
                    RaycastHit2D[] hits = Physics2D.CircleCastAll(_position, _radius, dir, remaining, _castMask);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit2D h = hits[i];
                        if (h.collider == null || _ignoredThisStep.Contains(h.collider)) continue;
                        if (arena != null && h.collider == arena) continue;
                        int hb = 1 << h.collider.gameObject.layer;
                        if (h.distance <= 0f && (hb & _wallMask.value) != 0) continue;
                        if (!chosen.HasValue || h.distance < chosen.Value.distance) chosen = h;
                    }
                }

                float obstacleDist = chosen?.distance ?? float.PositiveInfinity;
                if (wallDist > remaining && obstacleDist > remaining)
                {
                    _position += dir * remaining;
                    remaining = 0f;
                    break;
                }
                if (wallDist <= obstacleDist)
                {
                    float travel = Mathf.Clamp(wallDist, 0f, remaining);
                    _position += dir * travel;
                    remaining -= travel;
                    _velocity = Vector2.Reflect(_velocity, wallNormal).normalized * _speed;
                    _position += wallNormal * SkinWidth;
                    result.BounceCountThisStep++;
                    // 손실 없는 바닥 판정(순서 무관 sticky): 바닥면(노멀 +y) 반사가 한 번이라도 있으면 수집 대상.
                    if (wallNormal.y >= FloorFaceNormalY) result.HitFloor = true;
                    continue;
                }
                RaycastHit2D hit = chosen.Value;
                float travelObs = Mathf.Max(hit.distance, 0f);
                _position += dir * travelObs;
                remaining -= travelObs;
                int hitLayerBit = 1 << hit.collider.gameObject.layer;
                bool isPassThrough = (hitLayerBit & _passThroughMask.value) != 0;
                if (isPassThrough)
                {
                    AddDamageHit(hit.collider);
                    _ignoredThisStep.Add(hit.collider);
                    continue;
                }
                _velocity = Vector2.Reflect(_velocity, hit.normal).normalized * _speed;
                _position += hit.normal * SkinWidth;
                result.BounceCountThisStep++;
                if ((hitLayerBit & _wallMask.value) == 0) AddDamageHit(hit.collider);
            }
            if (iterations < MaxIterationsPerStep || !(remaining > MinRemaining)) return result;
            // 스텝 예산 소진(코너 끼임 등) → 다음 스텝에서 벗어나도록 속도를 살짝 회전.
            _velocity = Quaternion.Euler(0, 0, 1.5f) * _velocity;
            return result;
        }

        // 이번 Step의 데미지 히트 기록. 같은 콜라이더는 한 번만(1접촉=1히트). 목록이 작아 선형 스캔으로 충분.
        private void AddDamageHit(Collider2D collider)
        {
            if (collider == null) return;
            for (int i = 0; i < _stepHits.Count; i++)
                if (_stepHits[i] == collider) return;
            _stepHits.Add(collider);
        }
    }
}
