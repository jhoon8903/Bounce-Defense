using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Motor
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private float radius = 0.15f;
        [SerializeField] private int previewBounces = 3;
        [SerializeField] private float maxPreviewDistance = 40f;
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private LayerMask blockMask;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
        }

        public void Draw(Vector2 origin, Vector2 direction)
        {
            List<Vector2> points = Simulate(origin, direction);
            _line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) _line.SetPosition(i, points[i]);
        }

        public void Hide()
        {
            if (_line) _line.positionCount = 0;
        }

        public List<Vector2> Simulate(Vector2 origin, Vector2 direction)
        {
            List<Vector2> points = new List<Vector2> { origin };
            Vector2 pos = origin;
            Vector2 dir = direction.normalized;
            int castMask = wallMask.value | enemyMask.value | blockMask.value;
            float remaining = maxPreviewDistance;
            int bounces = 0;
            Collider2D arena = wallMask.value != 0 ? Physics2D.OverlapPoint(pos, wallMask) : null;
            while (remaining > 0.001f && bounces < previewBounces)
            {
                float wallDist = float.PositiveInfinity;
                Vector2 wallNormal = Vector2.zero;
                if (arena != null) wallDist = MotorGeometry.DistanceToInnerWall(pos, dir, arena.bounds, radius, out wallNormal);
                RaycastHit2D obstacle = default;
                bool hasObstacle = false;
                var hits = Physics2D.CircleCastAll(pos, radius, dir, remaining, castMask);
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (arena != null && h.collider == arena) continue;
                    int hb = 1 << h.collider.gameObject.layer;
                    if (h.distance <= 0f && (hb & wallMask.value) != 0) continue;
                    if (hasObstacle && !(h.distance < obstacle.distance)) continue;
                    obstacle = h; hasObstacle = true;
                }

                float obstacleDist = hasObstacle ? obstacle.distance : float.PositiveInfinity;

                if (wallDist > remaining && obstacleDist > remaining)
                {
                    pos += dir * remaining;
                    points.Add(pos);
                    break;
                }

                if (wallDist <= obstacleDist)
                {
                    float travel = Mathf.Clamp(wallDist, 0f, remaining);
                    pos += dir * travel;
                    points.Add(pos);
                    remaining -= travel;
                    dir = Vector2.Reflect(dir, wallNormal).normalized;
                    pos += wallNormal * 0.01f;
                    bounces++;
                    continue;
                }

                pos += dir * obstacle.distance;
                points.Add(pos);
                remaining -= obstacle.distance;
                dir = Vector2.Reflect(dir, obstacle.normal).normalized;
                pos += obstacle.normal * 0.01f;
                bounces++;
            }

            return points;
        }
    }
}
