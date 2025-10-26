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
    private float headerWidth = 120f;
    private float cellWidth = 60f;
    private float cellHeight = 20f;

    private void OnGUI()
    {
        if (_table == null)
            _table = SOManager.Instance?.DamageTable;

        // Handle empty lists
        if (_table.attackTypes.Count == 0 || _table.rows.Count == 0)
        {
            EditorGUILayout.HelpBox("Damage table not initialized or missing data.", MessageType.Info);
            if (GUILayout.Button("Initialize Table"))
            {
                Undo.RecordObject(_table, "Initialize Damage Table");
                _table.InitializeTable();
                EditorUtility.SetDirty(_table);
            }
            return;
        }

        EditorGUILayout.LabelField("Damage Multiplier Table", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        float startX = 10 + headerWidth;
        float startY = 10 + headerHeight;

        // Top-left corner label
        EditorGUI.LabelField(
            new Rect(10, 10, headerWidth, headerHeight),
            "Defense ↓ / Attack →",
            EditorStyles.centeredGreyMiniLabel
        );

        // Draw attack type headers (columns)
        for (int x = 0; x < _table.attackTypes.Count; x++)
        {
            var atkType = _table.attackTypes[x];
            string atkLabel = atkType != null ? atkType.typeName : "(Null)";
            EditorGUI.LabelField(
                new Rect(startX + x * cellWidth, 10, cellWidth, headerHeight),
                atkLabel,
                EditorStyles.boldLabel
            );
        }

        // Draw defense rows
        for (int row = 0; row < _table.rows.Count; row++)
        {
            var rowData = _table.rows[row];
            if (rowData == null) continue;

            string defLabel = rowData.defenseType != null ? rowData.defenseType.typeName : "(Null)";
            EditorGUI.LabelField(
                new Rect(10, startY + row * cellHeight, headerWidth, cellHeight),
                defLabel,
                EditorStyles.boldLabel
            );

            // Draw multipliers (cells)
            for (int col = 0; col < rowData.multipliers.Count; col++)
            {
                float value = rowData.multipliers[col];
                float newValue = EditorGUI.FloatField(
                    new Rect(startX + col * cellWidth, startY + row * cellHeight, cellWidth, cellHeight),
                    value
                );

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

        // Rebuild table button
        if (GUILayout.Button("Rebuild Table"))
        {
            Undo.RecordObject(_table, "Rebuild Damage Table");
            _table.InitializeTable();
            EditorUtility.SetDirty(_table);
        }
    }
}
