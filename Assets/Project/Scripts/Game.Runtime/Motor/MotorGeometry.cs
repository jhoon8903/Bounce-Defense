using UnityEngine;

namespace Game.Runtime.Motor
{
    public static class MotorGeometry
    {
        private const float DirEps = 1e-6f;
        private const float CornerEps = 1e-4f;
        
        public static float DistanceToInnerWall(Vector2 p, Vector2 dir, Bounds bounds, float r, out Vector2 normal)
        {
            float minX = bounds.min.x + r, maxX = bounds.max.x - r;
            float minY = bounds.min.y + r, maxY = bounds.max.y - r;
            float tx = float.PositiveInfinity, ty = float.PositiveInfinity;
            Vector2 nx = Vector2.zero, ny = Vector2.zero;
            switch (dir.x)
            {
                case > DirEps:
                    tx = (maxX - p.x) / dir.x; nx = Vector2.left;
                    break;
                case < -DirEps:
                    tx = (minX - p.x) / dir.x; nx = Vector2.right;
                    break;
            }
            switch (dir.y)
            {
                case > DirEps:
                    ty = (maxY - p.y) / dir.y; ny = Vector2.down;
                    break;
                case < -DirEps:
                    ty = (minY - p.y) / dir.y; ny = Vector2.up;
                    break;
            }
            float t;
            if (!float.IsPositiveInfinity(tx) && !float.IsPositiveInfinity(ty) && Mathf.Abs(tx - ty) < CornerEps)
            {
                t = Mathf.Min(tx, ty);
                normal = (nx + ny).normalized;
            }
            else if (tx < ty)
            {
                t = tx;
                normal = nx;
            }
            else
            {
                t = ty;
                normal = ny;
            }
            return Mathf.Max(t, 0f);
        }
    }
}
