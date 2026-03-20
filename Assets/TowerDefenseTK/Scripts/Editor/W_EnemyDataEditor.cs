using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class EnemyDataEditor : EditorWindow
{
    [MenuItem("Tools/Enemy Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<EnemyDataEditor>("Enemy Data Editor");
    }

    private List<EnemySO> enemies = new List<EnemySO>();
    private Vector2 scrollPos;
    private Vector2 defenseScrollPos;

    // Layout
    private const float LabelWidth  = 155f;
    private const float FieldWidth  = 115f;

    // Tabs
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Enemy Stats", "Defense Types" };

    private void OnEnable()  => RefreshEnemies();

    // ─────────────────────────────────────────────────────────────
    //  Main GUI
    // ─────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawHeader();

        if (GUILayout.Button("Refresh", GUILayout.Height(28)))
            RefreshEnemies();

        if (enemies.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "No EnemySO assets found in the project.\n\nCreate one via  TD Toolkit / Units / Enemy.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
        EditorGUILayout.Space(10);

        if (selectedTab == 0)
            DrawStatsTab();
        else
            DrawDefenseTypesTab();

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Save All Changes to Assets", GUILayout.Height(40)))
            SaveAllChanges();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            $"{enemies.Count} EnemySO asset(s) found in project",
            EditorStyles.miniLabel);
    }

    // ─────────────────────────────────────────────────────────────
    //  Header
    // ─────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Enemy Data Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Editing all EnemySO assets found in the project",
            EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
    }

    // ─────────────────────────────────────────────────────────────
    //  Tab 1 – Stats spreadsheet
    // ─────────────────────────────────────────────────────────────
    private void DrawStatsTab()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Column headers
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Property", EditorStyles.boldLabel, GUILayout.Width(LabelWidth));
        foreach (var e in enemies)
            GUILayout.Label(e.name, EditorStyles.boldLabel, GUILayout.Width(FieldWidth));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // ── Core stats ───────────────────────────────────────────
        DrawFloatRow("Health",       e => e.Health,      (e, v) => e.Health      = v);
        DrawFloatRow("Speed",        e => e.Speed,       (e, v) => e.Speed       = v);
        DrawFloatRow("Gold Reward",  e => e.goldReward,  (e, v) => e.goldReward  = v);
        DrawFloatRow("Armor",        e => e.armor,       (e, v) => e.armor       = v);
        DrawIntRow  ("Damage to Base",e => e.damageToBase,(e,v) => e.damageToBase = v);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        DrawBoolRow("Is Flying",     e => e.isFlying,      (e, v) => e.isFlying      = v);
        DrawBoolRow("Is Targetable", e => e.isTargetable,  (e, v) => e.isTargetable  = v);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Damage Type", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        DrawObjectRow<DamageType>(
            "Damage Type",
            e => e.damageType,
            (e, v) => e.damageType = v);

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────
    //  Tab 2 – Defense Types (list per enemy)
    // ─────────────────────────────────────────────────────────────
    private void DrawDefenseTypesTab()
    {
        defenseScrollPos = EditorGUILayout.BeginScrollView(defenseScrollPos);

        EditorGUILayout.HelpBox(
            "Each enemy can have multiple defense types that affect damage multipliers from the Damage Table.",
            MessageType.Info);
        EditorGUILayout.Space(10);

        foreach (var enemy in enemies)
            DrawEnemyDefenseSection(enemy);

        EditorGUILayout.EndScrollView();
    }

    private void DrawEnemyDefenseSection(EnemySO enemy)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(enemy.name, EditorStyles.boldLabel, GUILayout.Width(200));

        if (GUILayout.Button("+ Add Defense Type", GUILayout.Width(150)))
        {
            Undo.RecordObject(enemy, "Add Defense Type");
            enemy.defenseTypes.Add(null);
            EditorUtility.SetDirty(enemy);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (enemy.defenseTypes == null || enemy.defenseTypes.Count == 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("No defense types assigned.", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }
        else
        {
            // Iterate in reverse so removal by index is safe
            for (int i = 0; i < enemy.defenseTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);

                DefenseType oldVal = enemy.defenseTypes[i];
                DefenseType newVal = (DefenseType)EditorGUILayout.ObjectField(
                    $"[{i}]", oldVal, typeof(DefenseType), false);

                if (newVal != oldVal)
                {
                    Undo.RecordObject(enemy, "Edit Defense Type");
                    enemy.defenseTypes[i] = newVal;
                    EditorUtility.SetDirty(enemy);
                }

                // Remove button
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    Undo.RecordObject(enemy, "Remove Defense Type");
                    enemy.defenseTypes.RemoveAt(i);
                    EditorUtility.SetDirty(enemy);
                    EditorGUILayout.EndHorizontal();
                    break; // exit loop; Unity redraws next frame
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);
    }

    // ─────────────────────────────────────────────────────────────
    //  Row helpers
    // ─────────────────────────────────────────────────────────────
    private void DrawFloatRow(string label,
                              System.Func<EnemySO, float> getter,
                              System.Action<EnemySO, float> setter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        foreach (var enemy in enemies)
        {
            float oldVal = getter(enemy);
            float newVal = EditorGUILayout.FloatField(oldVal, GUILayout.Width(FieldWidth));
            if (!Mathf.Approximately(oldVal, newVal))
            {
                Undo.RecordObject(enemy, $"Edit {label}");
                setter(enemy, newVal);
                EditorUtility.SetDirty(enemy);
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    private void DrawIntRow(string label,
                            System.Func<EnemySO, int> getter,
                            System.Action<EnemySO, int> setter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        foreach (var enemy in enemies)
        {
            int oldVal = getter(enemy);
            int newVal = EditorGUILayout.IntField(oldVal, GUILayout.Width(FieldWidth));
            if (oldVal != newVal)
            {
                Undo.RecordObject(enemy, $"Edit {label}");
                setter(enemy, newVal);
                EditorUtility.SetDirty(enemy);
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    private void DrawBoolRow(string label,
                             System.Func<EnemySO, bool> getter,
                             System.Action<EnemySO, bool> setter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        foreach (var enemy in enemies)
        {
            bool oldVal = getter(enemy);
            // Center the toggle in the column
            GUILayout.Space((FieldWidth - 18f) * 0.5f);
            bool newVal = EditorGUILayout.Toggle(oldVal, GUILayout.Width(18f));
            GUILayout.Space((FieldWidth - 18f) * 0.5f);

            if (oldVal != newVal)
            {
                Undo.RecordObject(enemy, $"Edit {label}");
                setter(enemy, newVal);
                EditorUtility.SetDirty(enemy);
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    private void DrawObjectRow<T>(string label,
                                  System.Func<EnemySO, T> getter,
                                  System.Action<EnemySO, T> setter)
                                  where T : Object
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth));

        foreach (var enemy in enemies)
        {
            T oldVal = getter(enemy);
            T newVal = (T)EditorGUILayout.ObjectField(oldVal, typeof(T), false, GUILayout.Width(FieldWidth));

            if (oldVal != newVal)
            {
                Undo.RecordObject(enemy, $"Edit {label}");
                setter(enemy, newVal);
                EditorUtility.SetDirty(enemy);
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    // ─────────────────────────────────────────────────────────────
    //  Refresh & Save
    // ─────────────────────────────────────────────────────────────
    private void RefreshEnemies()
    {
        enemies.Clear();

        string[] guids = AssetDatabase.FindAssets("t:EnemySO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemySO enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
            if (enemy != null) enemies.Add(enemy);
        }

        enemies = enemies.OrderBy(e => e.name).Distinct().ToList();
        Repaint();
    }

    private void SaveAllChanges()
    {
        foreach (var enemy in enemies)
            EditorUtility.SetDirty(enemy);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemyDataEditor] Saved {enemies.Count} EnemySO asset(s).");
    }
}
