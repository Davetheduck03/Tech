using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TowerDefenseTK;

/// <summary>
/// Always-on Scene-view overlay that draws the active map's tile layout
/// as coloured quads on the ground plane.
///
/// Works entirely in Edit mode — no need to select anything.
/// The overlay updates live as you paint tiles in the Grid Map Editor.
///
/// Toggle:   Tools → Toggle Map Scene Gizmo   (or the Scene toolbar button)
/// </summary>
[InitializeOnLoad]
public static class MapSceneGizmoDrawer
{
    // ── Prefs key ─────────────────────────────────────────────────────────────
    private const string PrefKey = "TowerDefenseTK.MapSceneGizmo.Enabled";

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set
        {
            EditorPrefs.SetBool(PrefKey, value);
            SceneView.RepaintAll();
        }
    }

    // ── Tile colours ──────────────────────────────────────────────────────────
    private static readonly Color ColEmpty     = new Color(0.55f, 0.55f, 0.55f, 0.18f);
    private static readonly Color ColPath      = new Color(0.76f, 0.60f, 0.30f, 0.45f);
    private static readonly Color ColBlocked   = new Color(0.18f, 0.18f, 0.18f, 0.60f);
    private static readonly Color ColBuildable = new Color(0.30f, 0.70f, 0.30f, 0.45f);
    private static readonly Color ColSpawn     = new Color(0.20f, 0.50f, 0.90f, 0.65f);
    private static readonly Color ColExit      = new Color(0.90f, 0.20f, 0.20f, 0.65f);
    private static readonly Color ColHybrid    = new Color(0.95f, 0.75f, 0.15f, 0.55f);

    private static readonly Color ColGridLine  = new Color(0f, 0f, 0f, 0.25f);
    private static readonly Color ColOutline   = new Color(0f, 0f, 0f, 0.45f);

    // ── GUID persistence (survives domain reload) ─────────────────────────────
    private const string GuidPrefKey = "TowerDefenseTK.MapSceneGizmo.MapGUID";

    // ── Cached tile lookup ────────────────────────────────────────────────────
    // Rebuilt whenever OnActiveMapChanged fires or the map changes.
    private static Dictionary<Vector2Int, TileType> _tileCache
        = new Dictionary<Vector2Int, TileType>();
    private static MapData      _cachedMap;
    private static GridManager  _cachedGM;          // refreshed each scene load

    // ── Constructor (called by [InitializeOnLoad]) ────────────────────────────
    static MapSceneGizmoDrawer()
    {
        SceneView.duringSceneGui           += OnSceneGUI;
        W_GridMapEditor.OnActiveMapChanged += OnEditorMapChanged;

        // After every domain reload, try to restore the last-used map
        EditorApplication.delayCall += RestoreFromPrefsOrScene;
    }

    private static void OnEditorMapChanged()
    {
        MapData map = W_GridMapEditor.ActiveMap;

        // Persist the GUID so we can restore it after a domain reload
        if (map != null)
        {
            string path = AssetDatabase.GetAssetPath(map);
            string guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(GuidPrefKey, guid);
        }

        RebuildCache(map);
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Called once after domain reload.
    /// Priority: W_GridMapEditor.ActiveMap → saved GUID → scene MapLoader → scene PathNodeGenerator.
    /// </summary>
    private static void RestoreFromPrefsOrScene()
    {
        // 1. Editor window already has a map (window was open before reload)
        if (W_GridMapEditor.ActiveMap != null)
        {
            RebuildCache(W_GridMapEditor.ActiveMap);
            SceneView.RepaintAll();
            return;
        }

        // 2. Restore from persisted GUID
        string savedGuid = EditorPrefs.GetString(GuidPrefKey, "");
        if (!string.IsNullOrEmpty(savedGuid))
        {
            string path = AssetDatabase.GUIDToAssetPath(savedGuid);
            if (!string.IsNullOrEmpty(path))
            {
                MapData m = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (m != null)
                {
                    RebuildCache(m);
                    SceneView.RepaintAll();
                    return;
                }
            }
        }

        // 3. Fallback: read from MapLoader in the scene
        MapLoader loader = Object.FindFirstObjectByType<MapLoader>();
        if (loader != null && loader.mapData != null)
        {
            RebuildCache(loader.mapData);
            SceneView.RepaintAll();
            return;
        }

        // 4. Fallback: read from PathNodeGenerator in the scene (private field via SerializedObject)
        PathNodeGenerator gen = Object.FindFirstObjectByType<PathNodeGenerator>();
        if (gen != null)
        {
            SerializedObject   so   = new SerializedObject(gen);
            SerializedProperty prop = so.FindProperty("mapData");
            MapData            m    = prop?.objectReferenceValue as MapData;
            if (m != null)
            {
                RebuildCache(m);
                SceneView.RepaintAll();
            }
        }
    }

    // ── Menu item ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Toggle Map Scene Gizmo")]
    private static void Toggle() => Enabled = !Enabled;

    [MenuItem("Tools/Toggle Map Scene Gizmo", validate = true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/Toggle Map Scene Gizmo", Enabled);
        return true;
    }

    // ── Cache ─────────────────────────────────────────────────────────────────
    private static void RebuildCache(MapData map = null)
    {
        // If no map passed in, prefer the editor's active map
        if (map == null) map = W_GridMapEditor.ActiveMap;

        _tileCache.Clear();
        _cachedMap = map;

        if (map == null) return;

        foreach (var td in map.tiles)
            _tileCache[td.coords] = td.type;
    }

    // ── Scene GUI ─────────────────────────────────────────────────────────────
    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!Enabled) return;

        // Use the cached map (restored from prefs/scene on reload)
        MapData map = _cachedMap;

        // If cache is empty, try a lazy restore once
        if (map == null)
        {
            RestoreFromPrefsOrScene();
            map = _cachedMap;
        }

        if (map == null) return;

        // If the editor window switched maps since last frame, re-cache
        if (W_GridMapEditor.ActiveMap != null && W_GridMapEditor.ActiveMap != _cachedMap)
        {
            RebuildCache(W_GridMapEditor.ActiveMap);
            map = _cachedMap;
        }

        // Resolve world-space origin and cell size from the in-scene GridManager
        // Re-find only when the cached reference goes stale (scene change, etc.)
        if (_cachedGM == null)
            _cachedGM = Object.FindFirstObjectByType<GridManager>();
        GridManager gm      = _cachedGM;
        float       cellSz  = gm != null ? gm.cellSize  : map.cellSize;
        Vector3     origin  = gm != null ? gm.transform.position : Vector3.zero;
        float       groundY = origin.y + 0.02f; // tiny lift to avoid z-fighting

        int w = map.width;
        int h = map.height;

        // ── Draw tile quads ───────────────────────────────────────────────────
        foreach (var kvp in _tileCache)
        {
            Vector2Int coords = kvp.Key;
            TileType   type   = kvp.Value;

            if (coords.x < 0 || coords.x >= w || coords.y < 0 || coords.y >= h) continue;

            Color fill    = GetFillColor(type);
            Color outline = type == TileType.Empty ? Color.clear : ColOutline;

            Vector3 bl = origin + new Vector3(coords.x * cellSz,        groundY, coords.y * cellSz);
            Vector3 br = origin + new Vector3((coords.x + 1) * cellSz,  groundY, coords.y * cellSz);
            Vector3 tr = origin + new Vector3((coords.x + 1) * cellSz,  groundY, (coords.y + 1) * cellSz);
            Vector3 tl = origin + new Vector3(coords.x * cellSz,        groundY, (coords.y + 1) * cellSz);

            Handles.DrawSolidRectangleWithOutline(
                new Vector3[] { bl, tl, tr, br },
                fill,
                outline);
        }

        // ── Draw grid lines ───────────────────────────────────────────────────
        Handles.color = ColGridLine;

        float totalW = w * cellSz;
        float totalH = h * cellSz;

        for (int x = 0; x <= w; x++)
        {
            float xPos = origin.x + x * cellSz;
            Handles.DrawLine(
                new Vector3(xPos, groundY, origin.z),
                new Vector3(xPos, groundY, origin.z + totalH));
        }

        for (int y = 0; y <= h; y++)
        {
            float zPos = origin.z + y * cellSz;
            Handles.DrawLine(
                new Vector3(origin.x,          groundY, zPos),
                new Vector3(origin.x + totalW, groundY, zPos));
        }

        // ── Draw tile-type labels (only when zoomed in) ───────────────────────
        DrawTileLabels(sceneView, origin, cellSz, groundY);

        // ── Draw legend in the Scene view corner ──────────────────────────────
        DrawSceneLegend(sceneView);
    }

    private static void DrawTileLabels(SceneView sv,
        Vector3 origin, float cellSz, float groundY)
    {
        // Estimate pixels-per-unit; skip labels when tiles are too small on screen
        float ppu = sv.size > 0 ? sv.position.height / (sv.size * 2f) : 0f;
        if (ppu < 20f) return;  // fewer than ~20 px per world unit → too small

        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };

        foreach (var kvp in _tileCache)
        {
            Vector2Int coords = kvp.Key;
            TileType   type   = kvp.Value;

            string label = type switch
            {
                TileType.Spawn     => "S",
                TileType.Exit      => "E",
                TileType.Hybrid    => "H",
                TileType.Blocked   => "X",
                TileType.Buildable => "B",
                TileType.Path      => "·",
                _                  => ""
            };

            if (string.IsNullOrEmpty(label)) continue;

            Vector3 centre = origin + new Vector3(
                (coords.x + 0.5f) * cellSz,
                groundY + 0.01f,
                (coords.y + 0.5f) * cellSz);

            Handles.Label(centre, label, style);
        }
    }

    // ── 2D legend in the Scene view's Handles GUI layer ──────────────────────
    private static void DrawSceneLegend(SceneView sv)
    {
        // Use Handles.BeginGUI / EndGUI to draw a 2D panel in Scene view
        Handles.BeginGUI();

        float panelW  = 110f;
        float rowH    = 16f;
        float padding = 6f;

        // Only show non-empty types
        var types = new[]
        {
            (TileType.Path,      "Path"),
            (TileType.Blocked,   "Blocked"),
            (TileType.Buildable, "Buildable"),
            (TileType.Spawn,     "Spawn (S)"),
            (TileType.Exit,      "Exit (E)"),
            (TileType.Hybrid,    "Hybrid (H)"),
        };

        float panelH = types.Length * rowH + padding * 2f + 20f; // +20 for header
        Rect  panel  = new Rect(8f, sv.position.height - panelH - 28f, panelW, panelH);

        // Semi-transparent background
        EditorGUI.DrawRect(panel, new Color(0.1f, 0.1f, 0.1f, 0.72f));

        // Header
        GUI.Label(new Rect(panel.x + 4f, panel.y + 4f, panelW - 8f, 16f),
            "Map Gizmo", EditorStyles.whiteBoldLabel);

        float y = panel.y + padding + 18f;

        foreach (var (type, name) in types)
        {
            Rect swatchRect = new Rect(panel.x + 4f, y + 2f, 10f, 10f);
            Rect labelRect  = new Rect(panel.x + 18f, y, panelW - 22f, rowH);

            Color col = GetFillColor(type);
            col.a = 1f;
            EditorGUI.DrawRect(swatchRect, col);
            GUI.Label(labelRect, name, EditorStyles.miniLabel);

            y += rowH;
        }

        Handles.EndGUI();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Color GetFillColor(TileType type) => type switch
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
}
