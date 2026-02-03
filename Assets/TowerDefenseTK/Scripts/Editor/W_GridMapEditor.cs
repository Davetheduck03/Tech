using UnityEngine;
using UnityEditor;
using TowerDefenseTK;

public class W_GridMapEditor : EditorWindow
{
    [MenuItem("Tools/Grid Map Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<W_GridMapEditor>("Grid Map Editor");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private MapData currentMap;
    private Vector2 scrollPos;
    private TileType selectedBrush = TileType.Path;
    private int brushSize = 1;

    // Visual settings
    private float cellDisplaySize = 25f;
    private float gridPadding = 10f;

    // Colors for each tile type
    private readonly Color emptyColor = new Color(0.3f, 0.3f, 0.3f);
    private readonly Color pathColor = new Color(0.6f, 0.5f, 0.3f);
    private readonly Color blockedColor = new Color(0.2f, 0.2f, 0.2f);
    private readonly Color buildableColor = new Color(0.3f, 0.6f, 0.3f);
    private readonly Color spawnColor = new Color(0.2f, 0.5f, 0.8f);
    private readonly Color exitColor = new Color(0.8f, 0.2f, 0.2f);

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        DrawHeader();
        EditorGUILayout.Space(5);

        DrawMapSelector();
        EditorGUILayout.Space(10);

        if (currentMap == null)
        {
            EditorGUILayout.HelpBox("Select or create a Map Data asset to begin editing.", MessageType.Info);
            DrawCreateNewMapButton();
            return;
        }

        DrawBrushSelector();
        EditorGUILayout.Space(10);

        DrawGridSettings();
        EditorGUILayout.Space(10);

        DrawGrid();
        EditorGUILayout.Space(10);

        DrawActions();
        EditorGUILayout.Space(10);

        DrawValidation();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Grid Map Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Design your tower defense map layout", EditorStyles.miniLabel);
    }

    private void DrawMapSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Map Data:", GUILayout.Width(70));

        MapData newMap = (MapData)EditorGUILayout.ObjectField(currentMap, typeof(MapData), false);

        if (newMap != currentMap)
        {
            currentMap = newMap;
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCreateNewMapButton()
    {
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create New Map", GUILayout.Height(30)))
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Map",
                "NewMap",
                "asset",
                "Choose where to save the map data"
            );

            if (!string.IsNullOrEmpty(path))
            {
                MapData newMap = ScriptableObject.CreateInstance<MapData>();
                newMap.InitializeEmpty();

                AssetDatabase.CreateAsset(newMap, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentMap = newMap;
                Selection.activeObject = newMap;
            }
        }
    }

    private void DrawBrushSelector()
    {
        EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Brush type buttons
        DrawBrushButton(TileType.Empty, "Empty", emptyColor);
        DrawBrushButton(TileType.Path, "Path", pathColor);
        DrawBrushButton(TileType.Blocked, "Blocked", blockedColor);
        DrawBrushButton(TileType.Buildable, "Build", buildableColor);
        DrawBrushButton(TileType.Spawn, "Spawn", spawnColor);
        DrawBrushButton(TileType.Exit, "Exit", exitColor);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush Size:", GUILayout.Width(70));
        brushSize = EditorGUILayout.IntSlider(brushSize, 1, 5);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrushButton(TileType type, string label, Color color)
    {
        Color oldBg = GUI.backgroundColor;

        if (selectedBrush == type)
        {
            GUI.backgroundColor = color;
        }

        if (GUILayout.Button(label, GUILayout.Width(60), GUILayout.Height(25)))
        {
            selectedBrush = type;
        }

        GUI.backgroundColor = oldBg;
    }

    private void DrawGridSettings()
    {
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Width:", GUILayout.Width(50));
        int newWidth = EditorGUILayout.IntField(currentMap.width, GUILayout.Width(50));
        EditorGUILayout.LabelField("Height:", GUILayout.Width(50));
        int newHeight = EditorGUILayout.IntField(currentMap.height, GUILayout.Width(50));
        EditorGUILayout.LabelField("Cell Size:", GUILayout.Width(60));
        float newCellSize = EditorGUILayout.FloatField(currentMap.cellSize, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(currentMap, "Change Grid Settings");

            // Resize if dimensions changed
            if (newWidth != currentMap.width || newHeight != currentMap.height)
            {
                if (EditorUtility.DisplayDialog("Resize Grid?",
                    "Changing grid size will clear tiles outside the new bounds. Continue?",
                    "Yes", "Cancel"))
                {
                    currentMap.width = Mathf.Max(1, newWidth);
                    currentMap.height = Mathf.Max(1, newHeight);
                    CleanupOutOfBoundsTiles();
                }
            }

            currentMap.cellSize = Mathf.Max(0.1f, newCellSize);
            EditorUtility.SetDirty(currentMap);
        }

        // Display cell size slider
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Size:", GUILayout.Width(80));
        cellDisplaySize = EditorGUILayout.Slider(cellDisplaySize, 15f, 40f);
        EditorGUILayout.EndHorizontal();
    }

    private void CleanupOutOfBoundsTiles()
    {
        currentMap.tiles.RemoveAll(t =>
            t.coords.x >= currentMap.width || t.coords.y >= currentMap.height ||
            t.coords.x < 0 || t.coords.y < 0);

        currentMap.spawnPoints.RemoveAll(p =>
            p.x >= currentMap.width || p.y >= currentMap.height ||
            p.x < 0 || p.y < 0);

        currentMap.exitPoints.RemoveAll(p =>
            p.x >= currentMap.width || p.y >= currentMap.height ||
            p.x < 0 || p.y < 0);
    }

    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Map Layout", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Click to paint, Right-click to erase", EditorStyles.miniLabel);

        float gridWidth = currentMap.width * cellDisplaySize + gridPadding * 2;
        float gridHeight = currentMap.height * cellDisplaySize + gridPadding * 2;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos,
            GUILayout.Height(Mathf.Min(gridHeight + 20, 400)));

        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
        gridRect.x += gridPadding;
        gridRect.y += gridPadding;

        // Draw cells (Y is inverted for natural top-down view)
        for (int x = 0; x < currentMap.width; x++)
        {
            for (int y = 0; y < currentMap.height; y++)
            {
                int displayY = currentMap.height - 1 - y; // Flip Y for display

                Rect cellRect = new Rect(
                    gridRect.x + x * cellDisplaySize,
                    gridRect.y + displayY * cellDisplaySize,
                    cellDisplaySize - 1,
                    cellDisplaySize - 1
                );

                Vector2Int coords = new Vector2Int(x, y);
                TileData tile = currentMap.GetTile(coords);
                TileType type = tile?.type ?? TileType.Empty;

                // Draw cell background
                EditorGUI.DrawRect(cellRect, GetTileColor(type));

                // Draw cell border
                Handles.color = Color.black;
                Handles.DrawWireDisc(cellRect.center, Vector3.forward, 0);

                // Draw spawn/exit labels
                if (type == TileType.Spawn || type == TileType.Exit)
                {
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(cellRect, type == TileType.Spawn ? "S" : "E", labelStyle);
                }

                // Handle mouse input
                HandleCellInput(cellRect, coords);
            }
        }

        EditorGUILayout.EndScrollView();

        // Draw legend
        DrawLegend();
    }

    private void HandleCellInput(Rect cellRect, Vector2Int coords)
    {
        Event e = Event.current;

        if (!cellRect.Contains(e.mousePosition)) return;

        // Left click - paint
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            PaintTile(coords);
            e.Use();
        }
        // Left drag - paint
        else if (e.type == EventType.MouseDrag && e.button == 0)
        {
            PaintTile(coords);
            e.Use();
        }
        // Right click - erase
        else if (e.type == EventType.MouseDown && e.button == 1)
        {
            EraseTile(coords);
            e.Use();
        }
        // Right drag - erase
        else if (e.type == EventType.MouseDrag && e.button == 1)
        {
            EraseTile(coords);
            e.Use();
        }
    }

    private void PaintTile(Vector2Int center)
    {
        Undo.RecordObject(currentMap, "Paint Tile");

        for (int dx = -brushSize + 1; dx < brushSize; dx++)
        {
            for (int dy = -brushSize + 1; dy < brushSize; dy++)
            {
                Vector2Int coords = new Vector2Int(center.x + dx, center.y + dy);

                if (coords.x >= 0 && coords.x < currentMap.width &&
                    coords.y >= 0 && coords.y < currentMap.height)
                {
                    currentMap.SetTile(coords, selectedBrush);
                }
            }
        }

        EditorUtility.SetDirty(currentMap);
        Repaint();
    }

    private void EraseTile(Vector2Int center)
    {
        Undo.RecordObject(currentMap, "Erase Tile");

        for (int dx = -brushSize + 1; dx < brushSize; dx++)
        {
            for (int dy = -brushSize + 1; dy < brushSize; dy++)
            {
                Vector2Int coords = new Vector2Int(center.x + dx, center.y + dy);

                if (coords.x >= 0 && coords.x < currentMap.width &&
                    coords.y >= 0 && coords.y < currentMap.height)
                {
                    currentMap.SetTile(coords, TileType.Empty);
                }
            }
        }

        EditorUtility.SetDirty(currentMap);
        Repaint();
    }

    private Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Empty => emptyColor,
            TileType.Path => pathColor,
            TileType.Blocked => blockedColor,
            TileType.Buildable => buildableColor,
            TileType.Spawn => spawnColor,
            TileType.Exit => exitColor,
            _ => emptyColor
        };
    }

    private void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem("Empty", emptyColor);
        DrawLegendItem("Path", pathColor);
        DrawLegendItem("Blocked", blockedColor);
        DrawLegendItem("Build", buildableColor);
        DrawLegendItem("Spawn", spawnColor);
        DrawLegendItem("Exit", exitColor);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLegendItem(string label, Color color)
    {
        Rect colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
        EditorGUI.DrawRect(colorRect, color);
        EditorGUILayout.LabelField(label, GUILayout.Width(45));
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear All", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Map?",
                "This will remove all tile data. Are you sure?",
                "Yes", "Cancel"))
            {
                Undo.RecordObject(currentMap, "Clear Map");
                currentMap.ClearAll();
                EditorUtility.SetDirty(currentMap);
            }
        }

        if (GUILayout.Button("Fill Empty", GUILayout.Height(25)))
        {
            Undo.RecordObject(currentMap, "Fill Empty");
            currentMap.InitializeEmpty();
            EditorUtility.SetDirty(currentMap);
        }

        if (GUILayout.Button("Apply to Scene", GUILayout.Height(25)))
        {
            ApplyMapToScene();
        }

        EditorGUILayout.EndHorizontal();

        // Player settings
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Starting Currency:", GUILayout.Width(110));
        currentMap.startingCurrency = EditorGUILayout.IntField(currentMap.startingCurrency, GUILayout.Width(60));
        EditorGUILayout.LabelField("Starting Lives:", GUILayout.Width(90));
        currentMap.startingLives = EditorGUILayout.IntField(currentMap.startingLives, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawValidation()
    {
        if (currentMap.Validate(out string error))
        {
            EditorGUILayout.HelpBox(
                $"Map is valid!\n" +
                $"Spawn Points: {currentMap.spawnPoints.Count}\n" +
                $"Exit Points: {currentMap.exitPoints.Count}\n" +
                $"Path Tiles: {currentMap.tiles.FindAll(t => t.type == TileType.Path).Count}",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private void ApplyMapToScene()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No GridManager found in scene! Please add one first.",
                "OK");
            return;
        }

        // Apply settings to GridManager
        Undo.RecordObject(gridManager, "Apply Map Data");
        gridManager.width = currentMap.width;
        gridManager.height = currentMap.height;
        gridManager.cellSize = currentMap.cellSize;

        // Store reference to map data
        MapLoader loader = gridManager.GetComponent<MapLoader>();
        if (loader == null)
        {
            loader = gridManager.gameObject.AddComponent<MapLoader>();
        }

        Undo.RecordObject(loader, "Set Map Data");
        loader.mapData = currentMap;

        EditorUtility.SetDirty(gridManager);
        EditorUtility.SetDirty(loader);

        Debug.Log($"Applied map '{currentMap.name}' to GridManager. Run the scene to see the result.");
    }
}