using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DamageTable))]
public class W_TowerManager : EditorWindow
{
    [MenuItem("Tools/TowerEditor")]
    public static void OnShowWindow()
    {
        var window = GetWindow<W_TowerManager>();
        window.titleContent = new GUIContent("Tower Editor");
        window.Show();
    }

    private DamageTable _table;
    private float spaceX = 50;
    private float spaceY = 50;
    private float width = 100;
    private float height = 50;

    private void OnGUI()
    {
        if (_table == null)
            _table = SOManager.Instance.DamageTable;

        float baseX = 0f;
        float baseY = 0f;

        GUIContent content = new GUIContent("Damage");
        EditorGUI.LabelField(new Rect(baseX, baseY, 100, 20), content);

        baseX += spaceX;
        for (int i = 0; i < _table.rows.Count; i++)
        {
            baseY += spaceY;

            content = new GUIContent(_table.rows[i].damageRow.typeName);
            EditorGUI.LabelField(new Rect(baseX, baseY, width, height), content);

            for (int j = 0; j < _table.rows[i].multipliers.Count; j++)
            {
                _table.rows[i].multipliers[j] = EditorGUI.FloatField(new Rect(baseX + 100f, baseY, width, height), _table.rows[i].multipliers[j]);
            }
        }
    }
}
