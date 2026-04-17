using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using TowerDefenseTK;

/// <summary>
/// Scene Setup Wizard — Tools ▶ Scene Setup Wizard
///
/// Creates a new (or configures the active) scene with the full standard
/// TowerDefenseTK hierarchy:
///
///   [Infrastructure]  Camera · Light · EventSystem
///   [Managers]        GameManager · EnemyManager · CurrencyManager ·
///                     PlayerHealthManager · TimeController ·
///                     TowerSelectionManager · Astar · PoolManager
///   [Map]             GridManager + MapLoader · PathNodeGenerator
///   [Canvas]          HUDCanvas (screen-space overlay)
/// </summary>
public class W_SceneSetupWizard : EditorWindow
{
    // ── Menu ──────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Scene Setup Wizard")]
    public static void ShowWindow()
    {
        var w = GetWindow<W_SceneSetupWizard>("Scene Setup Wizard");
        w.minSize = new Vector2(380, 440);
        w.Show();
    }

    // ── State ─────────────────────────────────────────────────────────────────
    private Vector2 _scroll;
    private string  _lastResult   = "";
    private bool    _resultIsError = false;

    // Scene
    private string _sceneName      = "Level_New";
    private bool   _createNewScene = true;

    // Map
    private MapData _mapData;
    private bool    _addPathNodeGenerator = true;

    // Infrastructure toggles
    private bool _addCamera        = true;
    private bool _addLight         = true;
    private bool _addEventSystem   = true;

    // Manager toggles
    private bool _addPoolManager      = true;
    private bool _addTowerSelection   = true;
    private bool _addPathVisualizer   = false;

    // Canvas
    private bool _addHUDCanvas = true;

    // PoolManager prefab pre-population
    private class PoolEntry
    {
        public GameObject prefab;
        public int        size = 10;
    }
    private readonly List<PoolEntry> _poolEntries = new List<PoolEntry>();

    // Layout
    private const float LabelWidth = 130f;
    private const float SizeWidth  =  52f;
    private const float RemoveWidth =  22f;

    // ── GUI ───────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawHeader();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSceneSection();
        DrawManagerSection();
        DrawMapSection();
        DrawInfrastructureSection();
        DrawPoolPrefabSection();

        GUILayout.Space(12);
        DrawBuildButton();
        DrawResultMessage();
        GUILayout.Space(8);

        EditorGUILayout.EndScrollView();
    }

    // ── Header ────────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Scene Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Scaffolds a new scene with the full TowerDefenseTK hierarchy.",
            EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(6);
        DrawHorizontalLine();
        GUILayout.Space(4);
    }

    // ── Scene section ─────────────────────────────────────────────────────────
    private void DrawSceneSection()
    {
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        _createNewScene = EditorGUILayout.ToggleLeft("Create New Scene", _createNewScene);

        if (_createNewScene)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scene Name", GUILayout.Width(LabelWidth));
            _sceneName = EditorGUILayout.TextField(_sceneName);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Objects will be injected into the currently active scene.",
                MessageType.Info);
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    // ── Managers section ──────────────────────────────────────────────────────
    private void DrawManagerSection()
    {
        EditorGUILayout.LabelField("Managers", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // Core managers — always included (shown greyed out)
        using (new EditorGUI.DisabledScope(true))
        {
            foreach (var name in new[] {
                "GameManager", "EnemyManager", "CurrencyManager",
                "PlayerHealthManager", "TimeController", "Astar" })
            {
                EditorGUILayout.ToggleLeft(name + "  (always included)", true);
            }
        }

        GUILayout.Space(4);

        _addPoolManager    = EditorGUILayout.ToggleLeft("PoolManager", _addPoolManager);
        _addTowerSelection = EditorGUILayout.ToggleLeft("TowerSelectionManager", _addTowerSelection);
        _addPathVisualizer = EditorGUILayout.ToggleLeft("PathVisualizer  (debug)", _addPathVisualizer);

        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    // ── Map section ───────────────────────────────────────────────────────────
    private void DrawMapSection()
    {
        EditorGUILayout.LabelField("Map", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // ObjectField — label + field in a horizontal row (proven pattern)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(
            new GUIContent("Map Data",
                "Optional. Auto-wires MapLoader and PathNodeGenerator.\n" +
                "Leave empty to configure GridManager manually after setup."),
            GUILayout.Width(LabelWidth));
        _mapData = (MapData)EditorGUILayout.ObjectField(
            _mapData, typeof(MapData), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);
        _addPathNodeGenerator = EditorGUILayout.ToggleLeft(
            new GUIContent("PathNodeGenerator",
                "Adds the runtime node grid. Assign nodePrefab after setup."),
            _addPathNodeGenerator);

        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    // ── Infrastructure section ────────────────────────────────────────────────
    private void DrawInfrastructureSection()
    {
        EditorGUILayout.LabelField("Infrastructure", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        _addCamera      = EditorGUILayout.ToggleLeft(
            new GUIContent("Main Camera", "Adds a top-down Camera tagged MainCamera."),
            _addCamera);
        _addLight       = EditorGUILayout.ToggleLeft(
            new GUIContent("Directional Light", "Adds a default directional light."),
            _addLight);
        _addEventSystem = EditorGUILayout.ToggleLeft(
            new GUIContent("EventSystem", "Required for UI interaction."),
            _addEventSystem);
        _addHUDCanvas   = EditorGUILayout.ToggleLeft(
            new GUIContent("HUD Canvas", "ScreenSpaceOverlay Canvas scaled to 1920×1080."),
            _addHUDCanvas);

        EditorGUILayout.EndVertical();
        GUILayout.Space(6);
    }

    // ── Pool prefab section ───────────────────────────────────────────────────
    private void DrawPoolPrefabSection()
    {
        if (!_addPoolManager) return;

        EditorGUILayout.LabelField("Pre-populate PoolManager  (optional)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // Column header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Prefab", EditorStyles.miniLabel);
        GUILayout.Label("Size", EditorStyles.miniLabel, GUILayout.Width(SizeWidth));
        GUILayout.Space(RemoveWidth);
        EditorGUILayout.EndHorizontal();

        // One row per entry — iterate backwards so removal doesn't skip indices
        int removeAt = -1;
        for (int i = 0; i < _poolEntries.Count; i++)
        {
            PoolEntry entry = _poolEntries[i];
            EditorGUILayout.BeginHorizontal();

            entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                entry.prefab, typeof(GameObject), false);

            entry.size = EditorGUILayout.IntField(
                entry.size, GUILayout.Width(SizeWidth));
            entry.size = Mathf.Max(1, entry.size);

            if (GUILayout.Button("−", GUILayout.Width(RemoveWidth)))
                removeAt = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
            _poolEntries.RemoveAt(removeAt);

        GUILayout.Space(2);
        if (GUILayout.Button("+ Add Pool Entry", GUILayout.Height(22)))
            _poolEntries.Add(new PoolEntry());

        if (_poolEntries.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No entries — PoolManager will start empty. You can call CreatePool() at runtime.",
                MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    // ── Build button ──────────────────────────────────────────────────────────
    private void DrawBuildButton()
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.35f, 0.70f, 0.35f);

        if (GUILayout.Button("✦  Create Scene", GUILayout.Height(36)))
            BuildScene();

        GUI.backgroundColor = prev;
    }

    private void DrawResultMessage()
    {
        if (string.IsNullOrEmpty(_lastResult)) return;
        GUILayout.Space(4);
        EditorGUILayout.HelpBox(_lastResult,
            _resultIsError ? MessageType.Error : MessageType.Info);
    }

    // ── Build logic ───────────────────────────────────────────────────────────
    private void BuildScene()
    {
        // 1. Create or verify the scene
        if (_createNewScene)
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                SetResult("Scene name cannot be empty.", error: true);
                return;
            }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save New Scene", _sceneName, "unity",
                "Choose where to save the scene.");

            if (string.IsNullOrEmpty(savePath))
            {
                SetResult("Scene creation cancelled.", error: false);
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _lastResult = savePath; // stash for save step
        }

        // 2. Infrastructure
        if (_addCamera)      CreateCamera();
        if (_addLight)       CreateDirectionalLight();
        if (_addEventSystem) CreateEventSystem();

        // 3. Managers container
        GameObject mgr = CreateContainer("Managers");
        CreateChild<GameManager>(mgr,         "GameManager");
        CreateChild<EnemyManager>(mgr,        "EnemyManager");
        CreateChild<CurrencyManager>(mgr,     "CurrencyManager");
        CreateChild<PlayerHealthManager>(mgr, "PlayerHealthManager");
        CreateChild<TimeController>(mgr,      "TimeController");
        CreateChild<Astar>(mgr,               "Astar");

        if (_addTowerSelection)
            CreateChild<TowerSelectionManager>(mgr, "TowerSelectionManager");
        if (_addPathVisualizer)
            CreateChild<PathVisualizer>(mgr, "PathVisualizer");

        // 4. PoolManager
        if (_addPoolManager)
        {
            GameObject poolGO = new GameObject("PoolManager");
            PoolManager pool  = poolGO.AddComponent<PoolManager>();
            WirePoolManager(pool);
            Undo.RegisterCreatedObjectUndo(poolGO, "Create PoolManager");
        }

        // 5. Map container
        GameObject mapGO  = CreateContainer("Map");
        GameObject gridGO = new GameObject("GridManager");
        gridGO.transform.SetParent(mapGO.transform);
        GridManager gridMgr  = gridGO.AddComponent<GridManager>();
        MapLoader   mapLoader = gridGO.AddComponent<MapLoader>();

        if (_mapData != null)
        {
            SerializedObject soLoader = new SerializedObject(mapLoader);
            soLoader.FindProperty("mapData").objectReferenceValue = _mapData;
            soLoader.ApplyModifiedProperties();

            SerializedObject soGrid = new SerializedObject(gridMgr);
            soGrid.FindProperty("width").intValue      = _mapData.width;
            soGrid.FindProperty("height").intValue     = _mapData.height;
            soGrid.FindProperty("cellSize").floatValue = _mapData.cellSize;
            soGrid.ApplyModifiedProperties();
        }
        Undo.RegisterCreatedObjectUndo(gridGO, "Create GridManager");

        if (_addPathNodeGenerator)
        {
            GameObject pngGO = new GameObject("PathNodeGenerator");
            pngGO.transform.SetParent(mapGO.transform);
            PathNodeGenerator png = pngGO.AddComponent<PathNodeGenerator>();

            if (_mapData != null)
            {
                SerializedObject soPng = new SerializedObject(png);
                soPng.FindProperty("mapData").objectReferenceValue = _mapData;
                soPng.ApplyModifiedProperties();
            }
            Undo.RegisterCreatedObjectUndo(pngGO, "Create PathNodeGenerator");
        }

        // 6. HUD Canvas
        if (_addHUDCanvas) CreateHUDCanvas();

        // 7. Save
        if (_createNewScene && !string.IsNullOrEmpty(_lastResult))
        {
            string savePath = _lastResult;
            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(), savePath);
            AssetDatabase.Refresh();
            SetResult($"Scene '{_sceneName}' created and saved to {savePath}", error: false);
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SetResult("Hierarchy built. Don't forget to save (Ctrl+S).", error: false);
        }
    }

    // ── Factory helpers ───────────────────────────────────────────────────────
    private static GameObject CreateContainer(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static T CreateChild<T>(GameObject parent, string name) where T : Component
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform);
        T comp   = go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return comp;
    }

    private static void CreateCamera()
    {
        if (Camera.main != null) return;
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        cam.fieldOfView     = 60f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(5f, 12f, -6f);
        camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
    }

    private static void CreateDirectionalLight()
    {
        var lightGO = new GameObject("Directional Light");
        var light   = lightGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        light.color     = new Color(1f, 0.95f, 0.84f);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Directional Light");
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
    }

    private static void CreateHUDCanvas()
    {
        var canvasGO = new GameObject("HUDCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUDCanvas");
    }

    private void WirePoolManager(PoolManager pool)
    {
        var valid = _poolEntries.FindAll(e => e.prefab != null);
        if (valid.Count == 0) return;

        SerializedObject   so       = new SerializedObject(pool);
        SerializedProperty itemList = so.FindProperty("poolItems");

        for (int i = 0; i < valid.Count; i++)
        {
            itemList.InsertArrayElementAtIndex(i);
            SerializedProperty item = itemList.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("prefab").objectReferenceValue = valid[i].prefab;
            item.FindPropertyRelative("size").intValue               = valid[i].size;
        }

        so.ApplyModifiedProperties();
    }

    // ── Utilities ─────────────────────────────────────────────────────────────
    private static void DrawHorizontalLine()
    {
        Rect r = GUILayoutUtility.GetRect(float.MaxValue, 1f);
        EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f));
    }

    private void SetResult(string msg, bool error)
    {
        _lastResult    = msg;
        _resultIsError = error;
        Repaint();
    }
}
