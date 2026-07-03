using UnityEngine;

namespace Game.Runtime.Grid
{
    // BallConfig와 동일 방식으로 RegisterInstance 주입되는 그리드 치수 설정.
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Game/Configs/GridConfig")]
    public sealed class GridConfig : ScriptableObject
    {
        [Header("Dimensions (cells)")]
        [SerializeField] [Min(1)] private int cols = 9;
        [SerializeField] [Min(1)] private int rows = 13;
        [SerializeField] [Min(0.01f)] private float cellSize = 1f;

        [Header("Anchor")]
        [Tooltip("씬 앵커 미배선 시 사용할 그리드 중심 월드 좌표. 씬의 빈 'Grid' 오브젝트는 (0,1.27).")]
        [SerializeField] private Vector2 originFallback = new(0f, 1.27f);

        public int Cols => cols;
        public int Rows => rows;
        public float CellSize => cellSize;
        public Vector2 OriginFallback => originFallback;
    }
}
