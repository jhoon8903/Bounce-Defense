using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    public sealed class KinematicRaycastMotor : IBallMotor
    {
        private const int MaxIterationsPerStep = 8;
        private const float SkinWidth = 0.01f;
        private const float MinRemaining = 0.0001f;
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
        private readonly HashSet<Collider2D> _passedThrough = new();
        private readonly List<BallHit> _stepHits = new();
        private readonly RaycastHit2D[] _castBuffer = new RaycastHit2D[16];
        private ContactFilter2D _castFilter;

        public Vector2 Position => _position;
        public IReadOnlyList<BallHit> LastStepHits => _stepHits;

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
            _castFilter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers, useLayerMask = true, layerMask = _castMask, useDepth = false };
            _passedThrough.Clear();
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
                    int count = Physics2D.CircleCast(_position, _radius, dir, _castFilter, _castBuffer, remaining);
                    for (int i = 0; i < count; i++)
                    {
                        RaycastHit2D h = _castBuffer[i];
                        if (h.collider == null || _ignoredThisStep.Contains(h.collider) || _passedThrough.Contains(h.collider)) continue;
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
                    result.WallBounceCountThisStep++;
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
                    AddDamageHit(hit.collider, -dir, hit.point);
                    _ignoredThisStep.Add(hit.collider);
                    _passedThrough.Add(hit.collider);
                    continue;
                }
                _velocity = Vector2.Reflect(_velocity, hit.normal).normalized * _speed;
                _position += hit.normal * SkinWidth;
                result.BounceCountThisStep++;
                if ((hitLayerBit & _wallMask.value) == 0) AddDamageHit(hit.collider, hit.normal, hit.point);
            }
            if (iterations < MaxIterationsPerStep || !(remaining > MinRemaining)) return result;
            _velocity = Quaternion.Euler(0, 0, 1.5f) * _velocity;
            return result;
        }

        private void AddDamageHit(Collider2D collider, Vector2 normal, Vector2 point)
        {
            if (collider == null) return;
            for (int i = 0; i < _stepHits.Count; i++)
            {
                if (_stepHits[i].Collider == collider) return;
            }
            _stepHits.Add(new BallHit(collider, normal, point));
        }
    }
}
