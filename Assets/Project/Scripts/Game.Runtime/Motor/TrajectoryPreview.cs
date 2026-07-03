using System.Collections.Generic;
using Game.Runtime.Combat;
using UnityEngine;

namespace Game.Runtime.Motor
{
    // 궤적 미리보기 = 둥근 dot 점선, 항상 표시. 단일 프로시저 메시(1 드로우콜) — 점당 GameObject 인스턴스 없음.
    // 원형 텍스처 quad를 dotSpacing 간격으로 배치. 월드 코너를 InverseTransformPoint로 로컬화 → Char 플립(scale.x=-1) 무관.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private LaunchController launchController;

        [Header("Simulation")]
        [SerializeField] private float radius = 0.15f;
        [SerializeField] private int previewBounces = 2;        // 첫 반사 세그먼트(꺾임 1개)까지
        [SerializeField] private float maxPreviewDistance = 40f;
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private LayerMask blockMask;

        [Header("Dots")]
        [SerializeField] private float dotSize = 0.16f;
        [SerializeField] private float dotSpacing = 0.42f;
        [SerializeField] private int maxDots = 120;
        [SerializeField] private Color dotColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private int sortingOrder = 50;

        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Mesh _mesh;
        private readonly List<Vector2> _path = new();
        private readonly List<Vector3> _verts = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<int> _tris = new();
        private readonly List<Color> _cols = new();

        private void Awake()
        {
            if (!launchController) launchController = FindObjectOfType<LaunchController>();
            _mf = GetComponent<MeshFilter>();
            if (!_mf) _mf = gameObject.AddComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            if (!_mr) _mr = gameObject.AddComponent<MeshRenderer>();
            _mesh = new Mesh { name = "TrajectoryDots" };
            _mesh.MarkDynamic();
            _mf.mesh = _mesh;
            Shader shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader != null ? shader : Shader.Find("Unlit/Transparent"));
            mat.mainTexture = BuildCircleTexture();
            _mr.sharedMaterial = mat;
            _mr.sortingOrder = sortingOrder;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (launchController == null) { _mesh.Clear(); return; }
            Simulate(launchController.Origin, launchController.CurrentDirection, _path);
            BuildDotMesh(_path);
        }

        private void BuildDotMesh(List<Vector2> path)
        {
            _verts.Clear(); _uvs.Clear(); _tris.Clear(); _cols.Clear();
            if (path.Count >= 2)
            {
                float half = dotSize * 0.5f;
                int count = 0;
                // 폴리라인을 dotSpacing 간격으로 리샘플 → 각 점에 quad.
                AddDot(path[0], half, ref count);
                float sinceLast = 0f;
                for (int seg = 0; seg < path.Count - 1 && count < maxDots; seg++)
                {
                    Vector2 a = path[seg], b = path[seg + 1];
                    float segLen = Vector2.Distance(a, b);
                    if (segLen < 1e-4f) continue;
                    Vector2 dir = (b - a) / segLen;
                    float d = dotSpacing - sinceLast;
                    while (d <= segLen && count < maxDots)
                    {
                        AddDot(a + dir * d, half, ref count);
                        d += dotSpacing;
                    }
                    sinceLast = segLen - (d - dotSpacing);
                }
            }
            _mesh.Clear();
            if (_verts.Count > 0)
            {
                _mesh.SetVertices(_verts);
                _mesh.SetUVs(0, _uvs);
                _mesh.SetColors(_cols);
                _mesh.SetTriangles(_tris, 0);
                _mesh.RecalculateBounds();
            }
        }

        private void AddDot(Vector2 worldCenter, float half, ref int count)
        {
            int b = _verts.Count;
            // 월드 코너 → 로컬(플립 보정). 스프라이트 셰이더는 Cull Off라 와인딩 무관.
            _verts.Add(transform.InverseTransformPoint(new Vector3(worldCenter.x - half, worldCenter.y - half, 0f)));
            _verts.Add(transform.InverseTransformPoint(new Vector3(worldCenter.x + half, worldCenter.y - half, 0f)));
            _verts.Add(transform.InverseTransformPoint(new Vector3(worldCenter.x + half, worldCenter.y + half, 0f)));
            _verts.Add(transform.InverseTransformPoint(new Vector3(worldCenter.x - half, worldCenter.y + half, 0f)));
            _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0)); _uvs.Add(new Vector2(1, 1)); _uvs.Add(new Vector2(0, 1));
            _cols.Add(dotColor); _cols.Add(dotColor); _cols.Add(dotColor); _cols.Add(dotColor);
            _tris.Add(b); _tris.Add(b + 1); _tris.Add(b + 2);
            _tris.Add(b); _tris.Add(b + 2); _tris.Add(b + 3);
            count++;
        }

        private static Texture2D BuildCircleTexture()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float a = Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) * 1.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // 벽/장애물 반사 궤적 시뮬(모터와 동일 규칙). previewBounces 회 반사까지 점 리스트.
        private void Simulate(Vector2 origin, Vector2 direction, List<Vector2> points)
        {
            points.Clear();
            points.Add(origin);
            Vector2 pos = origin;
            Vector2 dir = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.up;
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
                if (castMask != 0)
                {
                    RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, radius, dir, remaining, castMask);
                    foreach (RaycastHit2D h in hits)
                    {
                        if (h.collider == null) continue;
                        if (arena != null && h.collider == arena) continue;
                        int hb = 1 << h.collider.gameObject.layer;
                        if (h.distance <= 0f && (hb & wallMask.value) != 0) continue;
                        if (hasObstacle && !(h.distance < obstacle.distance)) continue;
                        obstacle = h; hasObstacle = true;
                    }
                }
                float obstacleDist = hasObstacle ? obstacle.distance : float.PositiveInfinity;
                if (wallDist > remaining && obstacleDist > remaining)
                {
                    pos += dir * remaining; points.Add(pos); break;
                }
                if (wallDist <= obstacleDist)
                {
                    float travel = Mathf.Clamp(wallDist, 0f, remaining);
                    pos += dir * travel; points.Add(pos); remaining -= travel;
                    dir = Vector2.Reflect(dir, wallNormal).normalized; pos += wallNormal * 0.01f; bounces++;
                    continue;
                }
                pos += dir * obstacle.distance; points.Add(pos); remaining -= obstacle.distance;
                dir = Vector2.Reflect(dir, obstacle.normal).normalized; pos += obstacle.normal * 0.01f; bounces++;
            }
        }
    }
}
