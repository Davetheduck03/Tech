using UnityEditor;
using UnityEngine;

public class W_DamageTableEditor : EditorWindow
{
    [MenuItem("Tools/Damage Table")]
    public static void ShowWindow()
    {
        var window = GetWindow<W_DamageTableEditor>();
        window.titleContent = new GUIContent("Damage Table");
        window.Show();
    }

    private DamageTable _table;
    private Vector2 scrollPos;

    private float headerHeight = 25f;
    private float headerWidth  = 120f;
    private float cellWidth    = 60f;
    private float cellHeight   = 20f;

    private void OnEnable() => FindTable();

    private void FindTable()
    {
        _table = null;
        string[] guids = AssetDatabase.FindAssets("t:DamageTable");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _table = AssetDatabase.LoadAssetAtPath<DamageTable>(path);
            if (guids.Length > 1)
                Debug.LogWarning("[DamageTableEditor] Multiple DamageTable assets found — using the first one.");
        }
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Damage Table", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Multiplier per Attack / Defense type pair", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Height(22)))
            FindTable();

        EditorGUI.BeginDisabledGroup(_table != null);
        if (GUILayout.Button("Create Table Asset", GUILayout.Height(22)))
            CreateTableAsset();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (_table == null)
        {
            EditorGUILayout.HelpBox(
                "No DamageTable asset found in the project.\n\nCreate one via  TD Toolkit / Damage Table,  or use the Create button above.",
                MessageType.Warning);
            return;
        }

        // Asset path for reference
        EditorGUILayout.LabelField(
            $"Asset: {AssetDatabase.GetAssetPath(_table)}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        // Handle empty / uninitialised table
        if (_table.attackTypes == null || _table.attackTypes.Count == 0 ||
            _table.rows == null        || _table.rows.Count == 0)
        {
            EditorGUILayout.HelpBox("Damage table not initialised or missing data.", MessageType.Info);
            if (GUILayout.Button("Initialize Table"))
            {
                Undo.RecordObject(_table, "Initialize Damage Table");
                _table.InitializeTable();
                EditorUtility.SetDirty(_table);
            }
            return;
        }

        // ── Grid ────────────────────────────────────────────────────────────

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        float startX = 10 + headerWidth;
        float startY = 10 + headerHeight;

        // Top-left corner label
        EditorGUI.LabelField(
            new Rect(10, 10, headerWidth, headerHeight),
            "Defense ↓ / Attack →",
            EditorStyles.centeredGreyMiniLabel);

        // Attack-type column headers
        for (int x = 0; x < _table.attackTypes.Count; x++)
        {
            var at = _table.attackTypes[x];
            EditorGUI.LabelField(
                new Rect(startX + x * cellWidth, 10, cellWidth, headerHeight),
                at != null ? at.typeName : "(Null)",
                EditorStyles.boldLabel);
        }

        // Defense-type row labels + multiplier cells
        for (int row = 0; row < _table.rows.Count; row++)
        {
            var rowData = _table.rows[row];
            if (rowData == null) continue;

            EditorGUI.LabelField(
                new Rect(10, startY + row * cellHeight, headerWidth, cellHeight),
                rowData.defenseType != null ? rowData.defenseType.typeName : "(Null)",
                EditorStyles.boldLabel);

            for (int col = 0; col < rowData.multipliers.Count; col++)
            {
                float value    = rowData.multipliers[col];
                float newValue = EditorGUI.FloatField(
                    new Rect(startX + col * cellWidth, startY + row * cellHeight, cellWidth, cellHeight),
                    value);

                if (!Mathf.Approximately(newValue, value))
                {
                    Undo.RecordObject(_table, "Edit Damage Multiplier");
                    rowData.multipliers[col] = newValue;
                    EditorUtility.SetDirty(_table);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.Space(_table.rows.Count * cellHeight + 40);

        // ── Actions ─────────────────────────────────────────────────────────

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Rebuild Table", GUILayout.Height(28)))
        {
            Undo.RecordObject(_table, "Rebuild Damage Table");
            _table.InitializeTable();
            EditorUtility.SetDirty(_table);
        }

        if (GUILayout.Button("Save", GUILayout.Height(28)))
        {
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (GUILayout.Button("Ping Asset", GUILayout.Height(28)))
            EditorGUIUtility.PingObject(_table);

        EditorGUILayout.EndHorizontal();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void CreateTableAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Damage Table", "DamageTable", "asset", "Choose where to save the DamageTable");
        if (string.IsNullOrEmpty(path)) return;

        DamageTable asset = ScriptableObject.CreateInstance<DamageTable>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _table = asset;
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
