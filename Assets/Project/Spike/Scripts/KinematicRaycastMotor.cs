using System.Collections.Generic;
using UnityEngine;

namespace Spike
{
    public sealed class KinematicRaycastMotor : IBallMotor
    {
        const int MaxIterationsPerStep = 8;
        const float SkinWidth = 0.01f;
        const float MinRemaining = 0.0001f;

        Vector2 _position;
        Vector2 _velocity;
        float _speed;
        float _radius;
        LayerMask _wallMask;
        LayerMask _enemyMask;
        LayerMask _blockMask;
        LayerMask _passThroughMask;
        LayerMask _castMask;
        readonly HashSet<Collider2D> _ignoredThisStep = new HashSet<Collider2D>();

        public Vector2 Position => _position;
        public Vector2 Velocity => _velocity;
        public float Speed => _speed;

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
            var result = new BallMotorStepResult();
            float remaining = _speed * deltaTime;
            int iterations = 0;
            _ignoredThisStep.Clear();

            while (remaining > MinRemaining && iterations < MaxIterationsPerStep)
            {
                iterations++;
                Vector2 dir = _velocity.normalized;
                var hits = Physics2D.CircleCastAll(_position, _radius, dir, remaining, _castMask);

                RaycastHit2D? chosen = null;
                foreach (var h in hits)
                {
                    if (h.collider == null || _ignoredThisStep.Contains(h.collider)) continue;
                    if (!chosen.HasValue || h.distance < chosen.Value.distance) chosen = h;
                }

                if (!chosen.HasValue)
                {
                    _position += dir * remaining;
                    remaining = 0f;
                    break;
                }

                var hit = chosen.Value;
                float travel = Mathf.Max(hit.distance, 0f);
                _position += dir * travel;
                remaining -= travel;

                int hitLayerBit = 1 << hit.collider.gameObject.layer;
                bool isPassThrough = (hitLayerBit & _passThroughMask.value) != 0;

                result.hitPoint = hit.point;
                result.hitNormal = hit.normal;
                result.hitCollider = hit.collider;

                if (isPassThrough)
                {
                    result.passedThrough = true;
                    if ((hitLayerBit & _blockMask.value) != 0) result.hitBlock = true;
                    else result.hitEnemy = true;
                    _ignoredThisStep.Add(hit.collider);
                    continue;
                }

                _velocity = Vector2.Reflect(_velocity, hit.normal).normalized * _speed;
                _position += hit.normal * SkinWidth;
                result.bounceCountThisStep++;

                if ((hitLayerBit & _wallMask.value) != 0) result.hitWall = true;
                else if ((hitLayerBit & _blockMask.value) != 0) result.hitBlock = true;
                else result.hitEnemy = true;
            }

            if (iterations >= MaxIterationsPerStep && remaining > MinRemaining)
            {
                // Anti-stuck: corner/groove loop ate the whole step budget. Drop the leftover
                // distance and nudge the angle so the next Step doesn't retrace the same loop.
                result.stuckAborted = true;
                _velocity = Quaternion.Euler(0, 0, 1.5f) * _velocity;
            }

            return result;
        }
    }
}
