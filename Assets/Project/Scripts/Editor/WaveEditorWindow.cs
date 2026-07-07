using System.Collections.Generic;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Stage;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public sealed class WaveEditorWindow : EditorWindow
    {
        private StageDefinition _stage;
        private WaveDefinition _wave;
        private int _waveIndex;
        private GridConfig _gridConfig;
        private EnemyDefinition _brush;
        private int _group;
        private bool _editInScene = true;
        private Vector2 _paletteScroll;
        private readonly List<EnemyDefinition> _enemyDefs = new();

        [MenuItem("Window/Bounce Defense/Wave Editor")]
        public static void Open() => GetWindow<WaveEditorWindow>("Wave Editor");

        private void OnEnable()
        {
            if (_gridConfig == null) _gridConfig = LoadFirst<GridConfig>();
            RefreshEnemyDefs();
            SceneView.duringSceneGui += OnSceneGui;
        }

        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGui;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Wave Editor", EditorStyles.boldLabel);

            _gridConfig = (GridConfig)EditorGUILayout.ObjectField("Grid Config", _gridConfig, typeof(GridConfig), false);

            EditorGUILayout.Space();
            _stage = (StageDefinition)EditorGUILayout.ObjectField("Stage", _stage, typeof(StageDefinition), false);
            if (_stage != null && _stage.WaveCount > 0)
            {
                _waveIndex = Mathf.Clamp(_waveIndex, 0, _stage.WaveCount - 1);
                _waveIndex = EditorGUILayout.IntSlider($"Wave (of {_stage.WaveCount})", _waveIndex + 1, 1, _stage.WaveCount) - 1;
                _wave = _stage.GetWave(_waveIndex);
            }
            _wave = (WaveDefinition)EditorGUILayout.ObjectField("Wave (edit target)", _wave, typeof(WaveDefinition), false);

            if (_wave == null)
            {
                EditorGUILayout.HelpBox("스테이지+웨이브를 고르거나 WaveDefinition을 직접 배정하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            _editInScene = EditorGUILayout.ToggleLeft("Edit in Scene View", _editInScene);
            EditorGUILayout.LabelField("배치 수(전체)", _wave.PlacementCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("서브웨이브 그룹", EditorStyles.boldLabel);
            _group = Mathf.Max(0, EditorGUILayout.IntField("현재 그룹 (0=1차, 1=2차 …)", _group));
            EditorGUILayout.LabelField("이 웨이브 그룹 수", _wave.GroupCount.ToString());
            EditorGUILayout.HelpBox("현재 그룹만 밝게 표시·편집됩니다. 다른 그룹은 흐리게(순차 스폰이라 같은 상단행에 겹쳐도 OK).", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Palette (브러시)", EditorStyles.boldLabel);
            if (GUILayout.Button("팔레트 새로고침", GUILayout.Width(120))) RefreshEnemyDefs();

            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll, GUILayout.MaxHeight(160));
            for (int i = 0; i < _enemyDefs.Count; i++)
            {
                EnemyDefinition def = _enemyDefs[i];
                if (def == null) continue;
                bool selected = _brush == def;
                GUI.backgroundColor = selected ? new Color(0.4f, 0.8f, 1f) : Color.white;
                if (GUILayout.Button($"{def.DisplayName}  [{def.Footprint.Width}x{def.Footprint.Height}]  HP {def.BaseHp}"))
                    _brush = selected ? null : def;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                _brush != null
                    ? $"브러시: {_brush.DisplayName} ({_brush.Footprint.Width}x{_brush.Footprint.Height})\n좌클릭=배치 · 좌/우클릭(점유 셀)=제거"
                    : "브러시를 고르면 Scene View에서 셀 클릭으로 배치할 수 있습니다.",
                MessageType.None);

            EditorGUILayout.Space();
            if (GUILayout.Button("이 웨이브 전체 비우기"))
            {
                if (EditorUtility.DisplayDialog("웨이브 비우기", $"'{_wave.name}'의 배치를 모두 지울까요?", "지움", "취소"))
                {
                    Undo.RecordObject(_wave, "Clear Wave");
                    _wave.EditorPlacements = new WaveDefinition.Placement[0];
                    EditorUtility.SetDirty(_wave);
                    SceneView.RepaintAll();
                }
            }
        }

        private void OnSceneGui(SceneView sv)
        {
            if (!_editInScene || _wave == null) return;
            GridGeometry geo = Geometry();
            Event e = Event.current;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlId);

            DrawGrid(geo);
            List<WaveDefinition.Placement> list = ReadPlacements();
            DrawPlacements(geo, list);

            if (!TryMouseCell(e, geo, out CellCoord cell))
            {
                sv.Repaint();
                return;
            }

            Footprint fp = _brush != null ? _brush.Footprint : Footprint.Size1x1;
            int hit = FindPlacementCovering(list, cell, _group);
            bool inBounds = geo.InBounds(cell.Col, cell.Row) &&
                            geo.InBounds(cell.Col + fp.Width - 1, cell.Row + fp.Height - 1);
            bool overlaps = Overlaps(list, cell, fp, -1, _group);

            if (_brush != null && hit < 0)
            {
                Color col = (inBounds && !overlaps) ? new Color(0.3f, 1f, 0.3f, 0.35f) : new Color(1f, 0.3f, 0.3f, 0.35f);
                DrawFootprint(geo, cell, fp, col, Color.white);
            }
            else if (hit >= 0)
            {
                WaveDefinition.Placement p = list[hit];
                DrawFootprint(geo, new CellCoord(p.col, p.row), PlacementFootprint(p), new Color(1f, 0.6f, 0.1f, 0.35f), Color.yellow);
            }

            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
            {
                if (e.button == 1 || hit >= 0)
                {
                    if (hit >= 0)
                    {
                        Undo.RecordObject(_wave, "Erase Placement");
                        list.RemoveAt(hit);
                        WritePlacements(list);
                    }
                }
                else if (_brush != null && inBounds && !overlaps)
                {
                    Undo.RecordObject(_wave, "Add Placement");
                    list.Add(new WaveDefinition.Placement { enemy = _brush, col = cell.Col, row = cell.Row, group = _group });
                    WritePlacements(list);
                }
                e.Use();
                Repaint();
            }

            sv.Repaint();
        }

        private static void DrawGrid(GridGeometry geo)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            for (int r = 0; r < geo.Rows; r++)
                for (int c = 0; c < geo.Cols; c++)
                {
                    Vector2 center = geo.CellToWorld(c, r);
                    float h = geo.CellSize * 0.5f;
                    Vector3[] v =
                    {
                        new(center.x - h, center.y - h, 0), new(center.x + h, center.y - h, 0),
                        new(center.x + h, center.y + h, 0), new(center.x - h, center.y + h, 0),
                    };
                    Handles.DrawSolidRectangleWithOutline(v, Color.clear, new Color(1f, 1f, 1f, 0.12f));
                }
        }

        private void DrawPlacements(GridGeometry geo, List<WaveDefinition.Placement> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                WaveDefinition.Placement p = list[i];
                if (p.enemy == null) continue;
                Footprint fp = PlacementFootprint(p);
                bool cur = p.group == _group;
                Color face = cur ? new Color(0.2f, 0.6f, 1f, 0.30f) : new Color(0.5f, 0.5f, 0.5f, 0.12f);
                Color outline = cur ? new Color(0.4f, 0.8f, 1f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.4f);
                DrawFootprint(geo, new CellCoord(p.col, p.row), fp, face, outline);
                if (cur)
                {
                    Vector2 center = geo.FootprintWorldCenter(new CellCoord(p.col, p.row), fp);
                    Handles.Label(center, $"{p.enemy.DisplayName}\n#{i} g{p.group}");
                }
            }
        }

        private static void DrawFootprint(GridGeometry geo, CellCoord anchor, Footprint fp, Color face, Color outline)
        {
            Vector2 center = geo.FootprintWorldCenter(anchor, fp);
            Vector2 size = geo.FootprintWorldSize(fp);
            float hx = size.x * 0.5f, hy = size.y * 0.5f;
            Vector3[] v =
            {
                new(center.x - hx, center.y - hy, 0), new(center.x + hx, center.y - hy, 0),
                new(center.x + hx, center.y + hy, 0), new(center.x - hx, center.y + hy, 0),
            };
            Handles.DrawSolidRectangleWithOutline(v, face, outline);
        }

        private GridGeometry Geometry()
        {
            int cols = _gridConfig != null ? _gridConfig.Cols : 9;
            int rows = _gridConfig != null ? _gridConfig.Rows : 13;
            float cell = _gridConfig != null ? _gridConfig.CellSize : 1f;
            Vector2 origin = _gridConfig != null ? _gridConfig.OriginFallback : new Vector2(0f, 1.27f);
            GameObject gridGo = GameObject.Find("Grid");
            if (gridGo != null) origin = gridGo.transform.position;
            return new GridGeometry(cols, rows, cell, origin);
        }

        private static bool TryMouseCell(Event e, GridGeometry geo, out CellCoord cell)
        {
            cell = default;
            Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Mathf.Abs(r.direction.z) < 1e-6f) return false;
            float t = -r.origin.z / r.direction.z;
            Vector3 world = r.origin + r.direction * t;
            cell = geo.WorldToCell(world);
            return true;
        }

        private static Footprint PlacementFootprint(WaveDefinition.Placement p) =>
            p.enemy != null ? p.enemy.Footprint : Footprint.Size1x1;

        private static int FindPlacementCovering(List<WaveDefinition.Placement> list, CellCoord cell, int group)
        {
            for (int i = 0; i < list.Count; i++)
            {
                WaveDefinition.Placement p = list[i];
                if (p.enemy == null || p.group != group) continue;
                Footprint fp = PlacementFootprint(p);
                if (cell.Col >= p.col && cell.Col < p.col + fp.Width &&
                    cell.Row >= p.row && cell.Row < p.row + fp.Height)
                    return i;
            }
            return -1;
        }

        private static bool Overlaps(List<WaveDefinition.Placement> list, CellCoord anchor, Footprint fp, int ignoreIndex, int group)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i == ignoreIndex) continue;
                WaveDefinition.Placement p = list[i];
                if (p.enemy == null || p.group != group) continue;
                Footprint ofp = PlacementFootprint(p);
                bool sepX = anchor.Col + fp.Width <= p.col || p.col + ofp.Width <= anchor.Col;
                bool sepY = anchor.Row + fp.Height <= p.row || p.row + ofp.Height <= anchor.Row;
                if (!sepX && !sepY) return true;
            }
            return false;
        }

        private List<WaveDefinition.Placement> ReadPlacements()
        {
            var list = new List<WaveDefinition.Placement>();
            WaveDefinition.Placement[] arr = _wave.EditorPlacements;
            if (arr != null) list.AddRange(arr);
            return list;
        }

        private void WritePlacements(List<WaveDefinition.Placement> list)
        {
            _wave.EditorPlacements = list.ToArray();
            EditorUtility.SetDirty(_wave);
        }

        private void RefreshEnemyDefs()
        {
            _enemyDefs.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (def != null) _enemyDefs.Add(def);
            }
        }

        private static T LoadFirst<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T obj = AssetDatabase.LoadAssetAtPath<T>(path);
                if (obj != null) return obj;
            }
            return null;
        }
    }
}
