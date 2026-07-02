using System.Collections.Generic;
using UnityEngine;

namespace Spike
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] float radius = 0.15f;
        [SerializeField] int previewBounces = 3;
        [SerializeField] float maxPreviewDistance = 40f;
        [SerializeField] LayerMask wallMask;
        [SerializeField] LayerMask enemyMask;
        [SerializeField] LayerMask blockMask;

        LineRenderer _line;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
        }

        public void Draw(Vector2 origin, Vector2 direction)
        {
            var points = Simulate(origin, direction);
            _line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                _line.SetPosition(i, points[i]);
        }

        // Same Reflect+skin-width math as KinematicRaycastMotor.Step, so the preview
        // is a real simulation rather than an approximation — required for §1 game-feel axis.
        public List<Vector2> Simulate(Vector2 origin, Vector2 direction)
        {
            var points = new List<Vector2> { origin };
            Vector2 pos = origin;
            Vector2 dir = direction.normalized;
            int castMask = wallMask.value | enemyMask.value | blockMask.value;
            float remaining = maxPreviewDistance;
            int bounces = 0;

            while (remaining > 0.001f && bounces < previewBounces)
            {
                var hit = Physics2D.CircleCast(pos, radius, dir, remaining, castMask);
                if (hit.collider == null)
                {
                    pos += dir * remaining;
                    points.Add(pos);
                    break;
                }

                pos += dir * hit.distance;
                points.Add(pos);
                remaining -= hit.distance;
                dir = Vector2.Reflect(dir, hit.normal).normalized;
                pos += hit.normal * 0.01f;
                bounces++;
            }

            return points;
        }
    }
}
