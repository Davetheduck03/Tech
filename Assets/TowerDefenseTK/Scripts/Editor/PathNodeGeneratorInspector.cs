using UnityEngine;
using UnityEditor;
using TowerDefenseTK;

/// <summary>
/// Custom Inspector for PathNodeGenerator.
/// Draws a colour-coded 2D grid preview directly in the Inspector
/// using the assigned MapData, so you can see the full tile layout
/// without entering Play mode or opening the Grid Map Editor window.
/// </summary>
[CustomEditor(typeof(PathNodeGenerator))]
public class PathNodeGeneratorInspector : Editor
{
    // ── Serialized property refs ──────────────────────────────────────────────
    private SerializedProperty _mapDataProp;
    private SerializedProperty _showDebugGizmosProp;

    // ── Tile colours (matches W_GridMapEditor palette) ────────────────────────
    private static readonly Color ColEmpty     = new Color(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color ColPath      = new Color(0.76f, 0.60f, 0.30f, 1f);
    private static readonly Color ColBlocked   = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color ColBuildable = new Color(0.35f, 0.70f, 0.35f, 1f);
    private static readonly Color ColSpawn     = new Color(0.25f, 0.55f, 0.90f, 1f);
    private static readonly Color ColExit      = new Color(0.90f, 0.25f, 0.25f, 1f);
    private static readonly Color ColHybrid    = new Color(0.95f, 0.75f, 0.20f, 1f);
    private static readonly Color ColGrid      = new Color(0f,   0f,   0f,   0.35f);

    // ── State ────────────────────────────────────────────────────────────────
    private bool _showPreview = true;
    private Vector2 _scrollPos;

    // ── GUIStyles (created lazily) ────────────────────────────────────────────
    private GUIStyle _centeredLabel;
    private GUIStyle _boldFoldout;

    private void OnEnable()
    {
        _mapDataProp         = serializedObject.FindProperty("mapData");
        _showDebugGizmosProp = serializedObject.FindProperty("showDebugGizmos");

        // Repaint when the active editor map changes so the preview stays fresh
        W_GridMapEditor.OnActiveMapChanged += Repaint;
    }

    private void OnDisable()
    {
        W_GridMapEditor.OnActiveMapChanged -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Active Editor Map sync strip ──────────────────────────────────────
        EnsureStyles();
        DrawEditorMapSyncStrip();

        EditorGUILayout.Space(4);

        // Draw the standard inspector fields
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(6);

        // ── Grid Preview foldout ──────────────────────────────────────────────
        _showPreview = EditorGUILayout.Foldout(_showPreview, "Grid Preview", true, _boldFoldout);
        if (!_showPreview) return;

        MapData mapData = _mapDataProp.objectReferenceValue as MapData;

        if (mapData == null)
        {
            EditorGUILayout.HelpBox("Assign a MapData asset to see the grid preview.", MessageType.Info);
            return;
        }

        int w = mapData.width;
        int h = mapData.height;

        if (w <= 0 || h <= 0)
        {
            EditorGUILayout.HelpBox("MapData has invalid dimensions.", MessageType.Warning);
            return;
        }

        // ── Compute cell pixel size so the grid fits the inspector width ──────
        float inspectorWidth  = EditorGUIUtility.currentViewWidth - 32f;  // margins
        const float kMinCell  = 8f;
        const float kMaxCell  = 28f;
        float cellPx = Mathf.Clamp(inspectorWidth / w, kMinCell, kMaxCell);

        float gridW = w * cellPx;
        float gridH = h * cellPx;

        // Build a lookup for fast tile queries
        var tileMap = BuildTileMap(mapData);

        // ── Scrollable area (appears when grid is taller than ~300 px) ────────
        bool needsScroll = gridH > 300f;
        if (needsScroll) _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
            GUILayout.Height(Mathf.Min(gridH + 4f, 320f)));

        // Reserve a rect for the grid
        Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH,
            GUILayout.ExpandWidth(false));

        if (Event.current.type == EventType.Repaint)
        {
            DrawGridCells(gridRect, mapData, tileMap, w, h, cellPx);
            DrawGridLines(gridRect, w, h, cellPx);
            DrawGridLabels(gridRect, mapData, tileMap, w, h, cellPx);
        }

        if (needsScroll) EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // ── Tile stats ────────────────────────────────────────────────────────
        DrawTileStats(mapData);

        EditorGUILayout.Space(4);

        // ── Legend ────────────────────────────────────────────────────────────
        DrawLegend();

        // Repaint on mouse move so the hover cell highlights stay crisp
        if (Event.current.type == EventType.MouseMove)
            Repaint();
    }

    // ── Editor-map sync ───────────────────────────────────────────────────────

    private void DrawEditorMapSyncStrip()
    {
        MapData editorMap   = W_GridMapEditor.ActiveMap;
        MapData assignedMap = _mapDataProp.objectReferenceValue as MapData;
        bool    inSync      = editorMap != null && editorMap == assignedMap;

        // Background colour: green when in sync, amber when different, grey when no editor map
        Color bg = editorMap == null
            ? new Color(0.35f, 0.35f, 0.35f, 0.4f)
            : inSync
                ? new Color(0.20f, 0.65f, 0.20f, 0.35f)
                : new Color(0.85f, 0.60f, 0.10f, 0.40f);

        Rect strip = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(strip, bg);

        string statusText = editorMap == null
            ? "  Grid Map Editor: not open"
            : inSync
                ? $"  ✔  Grid Map Editor: {editorMap.name}  (in sync)"
                : $"  ⚠  Grid Map Editor: {editorMap.name}";

        EditorGUI.LabelField(strip, statusText, EditorStyles.boldLabel);

        // Sync button — only shown when out of sync
        if (editorMap != null && !inSync)
        {
            Rect btnRect = new Rect(strip.xMax - 130f, strip.y + 2f, 126f, 18f);
            if (GUI.Button(btnRect, "Use Editor Map"))
            {
                serializedObject.Update();
                _mapDataProp.objectReferenceValue = editorMap;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }

    // ── Drawing helpers ───────────────────────────────────────────────────────

    private void DrawGridCells(Rect gridRect, MapData mapData,
        System.Collections.Generic.Dictionary<Vector2Int, TileType> tileMap,
        int w, int h, float cellPx)
    {
        // Check if mouse hovers over a cell
        Vector2 mouse      = Event.current.mousePosition;
        bool   mouseInGrid = gridRect.Contains(mouse);
        int    hoverX      = mouseInGrid ? Mathf.FloorToInt((mouse.x - gridRect.x) / cellPx) : -1;
        int    hoverY      = mouseInGrid ? (h - 1 - Mathf.FloorToInt((mouse.y - gridRect.y) / cellPx)) : -1;

        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                TileType type = tileMap.TryGetValue(new Vector2Int(x, y), out TileType t)
                    ? t : TileType.Empty;

                Color baseCol = GetTileColor(type);

                // Brighten on hover
                bool isHovered = (x == hoverX && y == hoverY);
                if (isHovered) baseCol = Color.Lerp(baseCol, Color.white, 0.25f);

                Rect cellRect = new Rect(
                    gridRect.x + x * cellPx,
                    gridRect.y + (h - 1 - y) * cellPx,
                    cellPx,
                    cellPx);

                EditorGUI.DrawRect(cellRect, baseCol);
            }
        }
    }

    private void DrawGridLines(Rect gridRect, int w, int h, float cellPx)
    {
        Handles.color = ColGrid;

        // Vertical lines
        for (int x = 0; x <= w; x++)
        {
            float xPos = gridRect.x + x * cellPx;
            Handles.DrawLine(
                new Vector3(xPos, gridRect.y, 0),
                new Vector3(xPos, gridRect.y + h * cellPx, 0));
        }

        // Horizontal lines
        for (int y = 0; y <= h; y++)
        {
            float yPos = gridRect.y + y * cellPx;
            Handles.DrawLine(
                new Vector3(gridRect.x, yPos, 0),
                new Vector3(gridRect.x + w * cellPx, yPos, 0));
        }
    }

    private void DrawGridLabels(Rect gridRect, MapData mapData,
        System.Collections.Generic.Dictionary<Vector2Int, TileType> tileMap,
        int w, int h, float cellPx)
    {
        if (cellPx < 14f) return; // too small to draw text

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                TileType type = tileMap.TryGetValue(new Vector2Int(x, y), out TileType t)
                    ? t : TileType.Empty;

                string label = type switch
                {
                    TileType.Spawn     => "S",
                    TileType.Exit      => "E",
                    TileType.Hybrid    => "H",
                    TileType.Blocked   => "X",
                    TileType.Buildable => "B",
                    TileType.Path      => "P",
                    _                  => ""
                };

                if (string.IsNullOrEmpty(label)) continue;

                Rect cellRect = new Rect(
                    gridRect.x + x * cellPx,
                    gridRect.y + (h - 1 - y) * cellPx,
                    cellPx,
                    cellPx);

                GUI.Label(cellRect, label, _centeredLabel);
            }
        }
    }

    private void DrawTileStats(MapData mapData)
    {
        // Count each type
        int[] counts = new int[System.Enum.GetValues(typeof(TileType)).Length];
        foreach (var td in mapData.tiles)
            counts[(int)td.type]++;

        EditorGUILayout.LabelField("Tile Counts", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
            {
                int c = counts[(int)type];
                if (c == 0) continue;
                DrawStatChip(type.ToString(), c, GetTileColor(type));
            }
        }
    }

    private void DrawStatChip(string label, int count, Color col)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = col;
        GUILayout.Box($"{label[0]}: {count}",
            EditorStyles.miniButton,
            GUILayout.MinWidth(42f));
        GUI.backgroundColor = prev;
    }

    private void DrawLegend()
    {
        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLegendSwatch(TileType.Empty,     "Empty");
            DrawLegendSwatch(TileType.Path,      "Path");
            DrawLegendSwatch(TileType.Blocked,   "Blocked");
            DrawLegendSwatch(TileType.Buildable, "Buildable");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLegendSwatch(TileType.Spawn,  "Spawn (S)");
            DrawLegendSwatch(TileType.Exit,   "Exit (E)");
            DrawLegendSwatch(TileType.Hybrid, "Hybrid (H)");
        }
    }

    private void DrawLegendSwatch(TileType type, string name)
    {
        Color col  = GetTileColor(type);
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = col;
        GUILayout.Box(name, EditorStyles.miniButton, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = prev;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static System.Collections.Generic.Dictionary<Vector2Int, TileType>
        BuildTileMap(MapData mapData)
    {
        var dict = new System.Collections.Generic.Dictionary<Vector2Int, TileType>(mapData.tiles.Count);
        foreach (var td in mapData.tiles)
            dict[td.coords] = td.type;
        return dict;
    }

    public static Color GetTileColor(TileType type) => type switch
    {
        TileType.Empty     => ColEmpty,
        TileType.Path      => ColPath,
        TileType.Blocked   => ColBlocked,
        TileType.Buildable => ColBuildable,
        TileType.Spawn     => ColSpawn,
        TileType.Exit      => ColExit,
        TileType.Hybrid    => ColHybrid,
        _                  => ColEmpty
    };

    private void EnsureStyles()
    {
        if (_centeredLabel == null)
        {
            _centeredLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }

        if (_boldFoldout == null)
        {
            _boldFoldout = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }
}
