using UnityEngine;
using VContainer;

namespace Game.Runtime.Grid
{
    public sealed class GridDebugView : MonoBehaviour
    {
        [Header("Preview (edit-time lattice)")]
        [SerializeField] [Min(1)] private int cols = 9;
        [SerializeField] [Min(1)] private int rows = 13;
        [SerializeField] [Min(0.01f)] private float cellSize = 1f;

        [Header("Gizmo")]
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawOccupancy = true;
        [SerializeField] private Color lineColor = new(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color occupiedColor = new(1f, 0.35f, 0.2f, 0.45f);

        private GridController _controller;

        [Inject]
        public void Construct(GridController controller) => _controller = controller;

        private void OnDrawGizmos()
        {
            if (!drawGrid && !drawOccupancy) return;

            bool ready = Application.isPlaying && _controller != null && _controller.IsReady;
            int c = ready ? _controller.Cols : cols;
            int r = ready ? _controller.Rows : rows;
            float size = ready ? _controller.CellSize : cellSize;
            Vector2 origin = ready ? _controller.Origin : (Vector2)transform.position;

            float halfCol = (c - 1) * 0.5f;
            float halfRow = (r - 1) * 0.5f;

            if (drawGrid)
            {
                Gizmos.color = lineColor;
                Vector3 wire = new(size * 0.98f, size * 0.98f, 0.01f);
                for (int row = 0; row < r; row++)
                    for (int col = 0; col < c; col++)
                    {
                        Vector3 center = new(origin.x + (col - halfCol) * size, origin.y + (halfRow - row) * size, 0f);
                        Gizmos.DrawWireCube(center, wire);
                    }
            }

            if (drawOccupancy && ready)
            {
                Gizmos.color = occupiedColor;
                Vector3 fill = new(size * 0.9f, size * 0.9f, 0.01f);
                for (int row = 0; row < r; row++)
                    for (int col = 0; col < c; col++)
                        if (_controller.IsCellOccupied(col, row))
                        {
                            Vector2 w = _controller.CellToWorld(col, row);
                            Gizmos.DrawCube(new Vector3(w.x, w.y, 0f), fill);
                        }
            }
        }
    }
}
