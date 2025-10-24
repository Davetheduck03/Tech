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
    private float headerWidth = 100f;
    private float cellWidth = 60f;
    private float cellHeight = 20f;

    private void OnGUI()
    {
        if (_table == null)
            _table = SOManager.Instance?.DamageTable;

        if (_table == null)
        {
            EditorGUILayout.HelpBox("No DamageTable found in SOManager!", MessageType.Warning);
            return;
        }

        if (_table.attackTypes.Count == 0 || _table.defenseTypes.Count == 0)
        {
            if (GUILayout.Button("Initialize Table"))
                _table.InitializeTable();
            return;
        }

        EditorGUILayout.LabelField("Damage Multiplier Table", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        float startX = 10 + headerWidth;
        float startY = 10 + headerHeight;

        // Draw top-left corner label
        EditorGUI.LabelField(new Rect(10, 10, headerWidth, headerHeight), "Defense ↓ / Attack →", EditorStyles.centeredGreyMiniLabel);

        // Draw attack type headers (columns)
        for (int x = 0; x < _table.attackTypes.Count; x++)
        {
            var atkType = _table.attackTypes[x];
            EditorGUI.LabelField(new Rect(startX + x * cellWidth, 10, cellWidth, headerHeight), atkType.typeName, EditorStyles.boldLabel);
        }

        // Draw defense type rows
        for (int row = 0; row < _table.rows.Count; row++)
        {
            var rowData = _table.rows[row];

            // Defense type label (row header)
            EditorGUI.LabelField(new Rect(10, startY + row * cellHeight, headerWidth, cellHeight), rowData.defenseType.typeName, EditorStyles.boldLabel);

            // Multipliers (columns = attacks)
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

        GUILayout.Space(_table.defenseTypes.Count * cellHeight + 40);

        if (GUILayout.Button("Rebuild Table"))
        {
            Undo.RecordObject(_table, "Rebuild Damage Table");
            _table.InitializeTable();
            EditorUtility.SetDirty(_table);
        }
    }
}
