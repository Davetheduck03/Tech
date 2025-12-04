using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TowerDataEditor : EditorWindow
{
    [MenuItem("Tools/Tower Data Editor (SO Manager)")]
    public static void ShowWindow()
    {
        GetWindow<TowerDataEditor>("Tower Data Editor");
    }

    private List<TowerSO> towers = new List<TowerSO>();
    private Vector2 scrollPos;

    // Layout
    private const float LabelWidth = 150f;
    private const float FieldWidth = 110f;
    private const float RowHeight = 20f;

    private void OnEnable()
    {
        RefreshTowersFromManager();
    }

    private void OnGUI()
    {
        DrawHeader();

        if (GUILayout.Button("Refresh from SOManager", GUILayout.Height(30)))
        {
            RefreshTowersFromManager();
        }

        if (towers.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("No towers found in SOManager.towers\n\nMake sure SOManager is in Resources/Data/SOManager.asset and has towers assigned.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);

        // Scrollable Table
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Header: Tower Names
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Property", EditorStyles.boldLabel, GUILayout.Width(LabelWidth));
        foreach (var tower in towers)
        {
            GUILayout.Label(tower.name, EditorStyles.boldLabel, GUILayout.Width(FieldWidth));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Property Rows
        DrawFloatRow("Fire Rate", t => t.fireRate, (t, v) => t.fireRate = v);
        DrawFloatRow("Range", t => t.range, (t, v) => t.range = v);
        DrawFloatRow("Projectile Speed", t => t.projectileSpeed, (t, v) => t.projectileSpeed = v);
        DrawFloatRow("AOE Radius", t => t.AOE_Radius, (t, v) => t.AOE_Radius = v, t => t.towerType == TowerType.AoE);
        DrawIntRow("Build Cost", t => t.buildCost, (t, v) => t.buildCost = v);

        DrawEnumRow<TowerType>("Tower Type", t => t.towerType, (t, v) => t.towerType = v);
        DrawEnumRow<TargetType>("Target Priority", t => t.towerTargetType, (t, v) => t.towerTargetType = v);
        DrawEnumRow<TargetGroup>("Target Group", t => t.targetGroup, (t, v) => t.targetGroup = v);
        DrawEnumRow<AOEType>("AOE Type", t => t.AOEType, (t, v) => t.AOEType = v, t => t.towerType == TowerType.AoE);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(15);

        // Save Button
        if (GUILayout.Button("Save All Changes to Assets", GUILayout.Height(40)))
        {
            SaveAllChanges();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"{towers.Count} tower(s) loaded from SOManager", EditorStyles.miniLabel);
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Tower Data Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Editing all towers from SOManager.towers", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
    }

    private void RefreshTowersFromManager()
    {
        towers.Clear();

        var manager = SOManager.Instance;
        if (manager == null)
        {
            Debug.LogError("SOManager not found! Make sure it's at Resources/Data/SOManager.asset");
            return;
        }

        if (manager.towers != null && manager.towers.Count > 0)
        {
            // Filter out nulls and duplicates
            towers = manager.towers
                .Where(t => t != null)
                .Distinct()
                .ToList();
        }

        Repaint();
    }

    private void SaveAllChanges()
    {
        foreach (var tower in towers)
        {
            EditorUtility.SetDirty(tower);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TowerDataEditor] Saved {towers.Count} towers from SOManager.");
    }

    private void DrawFloatRow(string label,
                         System.Func<TowerSO, float> getter,
                         System.Action<TowerSO, float> setter,
                         System.Func<TowerSO, bool> showCondition = null)
    {
        bool anyVisible = towers.Any(t => showCondition == null || showCondition(t));
        if (!anyVisible) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            bool canEdit = showCondition == null || showCondition(tower);

            if (!canEdit)
            {
                EditorGUILayout.LabelField("—", GUILayout.Width(FieldWidth));
                continue;
            }

            float oldVal = getter(tower);
            float newVal = EditorGUILayout.FloatField(oldVal, GUILayout.Width(FieldWidth));

            if (!Mathf.Approximately(oldVal, newVal))
            {
                Undo.RecordObject(tower, $"Edit {label}");
                setter(tower, newVal);
                EditorUtility.SetDirty(tower);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    private void DrawIntRow(string label, System.Func<TowerSO, int> getter, System.Action<TowerSO, int> setter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            int oldVal = getter(tower);
            int newVal = EditorGUILayout.IntField(oldVal, GUILayout.Width(FieldWidth));

            if (oldVal != newVal)
            {
                Undo.RecordObject(tower, $"Edit {label}");
                setter(tower, newVal);
                EditorUtility.SetDirty(tower);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEnumRow<T>(string label,
                           System.Func<TowerSO, T> getter,
                           System.Action<TowerSO, T> setter,
                           System.Func<TowerSO, bool> showCondition = null)
                           where T : struct, System.Enum
    {
        // Decide whether this row should be visible at all
        bool anyVisible = towers.Any(t => showCondition == null || showCondition(t));

        if (!anyVisible) return; // completely hide the row if no tower needs it

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            bool canEdit = showCondition == null || showCondition(tower);

            if (!canEdit)
            {
                // Show placeholder for non-matching towers (keeps columns aligned)
                EditorGUILayout.LabelField("—", GUILayout.Width(FieldWidth));
                continue;
            }

            T oldVal = getter(tower);
            EditorGUI.BeginChangeCheck();
            T newVal = (T)EditorGUILayout.EnumPopup(oldVal, GUILayout.Width(FieldWidth));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tower, $"Edit {label}");
                setter(tower, newVal);
                EditorUtility.SetDirty(tower);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }
}