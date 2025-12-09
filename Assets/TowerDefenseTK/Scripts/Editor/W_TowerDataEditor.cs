using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TowerDefenseTK;

public class TowerDataEditor : EditorWindow
{
    [MenuItem("Tools/Tower Data Editor (SO Manager)")]
    public static void ShowWindow()
    {
        GetWindow<TowerDataEditor>("Tower Data Editor");
    }

    private List<TowerSO> towers = new List<TowerSO>();
    private Vector2 scrollPos;
    private Vector2 upgradeScrollPos;

    // Layout
    private const float LabelWidth = 150f;
    private const float FieldWidth = 110f;
    private const float RowHeight = 20f;

    // Tabs
    private int selectedTab = 0;
    private string[] tabNames = { "Tower Stats", "Upgrade Paths" };

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

        // Tab Selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
        EditorGUILayout.Space(10);

        if (selectedTab == 0)
        {
            DrawStatsTab();
        }
        else if (selectedTab == 1)
        {
            DrawUpgradePathsTab();
        }

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

    private void DrawStatsTab()
    {
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
        DrawFloatRow("Damage", t => t.damage, (t, v) => t.damage = v);
        DrawFloatRow("Fire Rate", t => t.fireRate, (t, v) => t.fireRate = v);
        DrawFloatRow("Range", t => t.range, (t, v) => t.range = v);

        // Only show Projectile Speed for Turret and AoE types (towers that shoot projectiles)
        DrawFloatRow("Projectile Speed", t => t.projectileSpeed, (t, v) => t.projectileSpeed = v,
            t => t.towerType == TowerType.Turret || t.towerType == TowerType.AoE);

        // Only show AOE Radius for AoE towers
        DrawFloatRow("AOE Radius", t => t.AOE_Radius, (t, v) => t.AOE_Radius = v,
            t => t.towerType == TowerType.AoE);

        DrawIntRow("Build Cost", t => t.buildCost, (t, v) => t.buildCost = v);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tower Type Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        DrawEnumRow<TowerType>("Tower Type", t => t.towerType, (t, v) => t.towerType = v);
        DrawEnumRow<TargetType>("Target Priority", t => t.towerTargetType, (t, v) => t.towerTargetType = v);
        DrawEnumRow<TargetGroup>("Target Group", t => t.targetGroup, (t, v) => t.targetGroup = v);

        // Only show AOE Type for AoE towers
        DrawEnumRow<AOEType>("AOE Type", t => t.AOEType, (t, v) => t.AOEType = v,
            t => t.towerType == TowerType.AoE);

        EditorGUILayout.EndScrollView();
    }

    private void DrawUpgradePathsTab()
    {
        upgradeScrollPos = EditorGUILayout.BeginScrollView(upgradeScrollPos);

        EditorGUILayout.HelpBox("Configure upgrade paths for each tower. Create TowerUpgradeData assets and assign them here.", MessageType.Info);
        EditorGUILayout.Space(10);

        foreach (var tower in towers)
        {
            DrawTowerUpgradeSection(tower);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTowerUpgradeSection(TowerSO tower)
    {
        EditorGUILayout.BeginVertical("box");

        // Tower Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(tower.name, EditorStyles.boldLabel, GUILayout.Width(200));

        // Quick create button
        if (GUILayout.Button("Create Upgrade Data", GUILayout.Width(150)))
        {
            CreateUpgradeDataForTower(tower);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Find or create TowerUpgradeData
        TowerUpgradeData upgradeData = FindUpgradeDataForTower(tower);

        if (upgradeData == null)
        {
            EditorGUILayout.HelpBox($"No TowerUpgradeData found for {tower.name}", MessageType.Warning);
        }
        else
        {
            // Show upgrade data
            SerializedObject so = new SerializedObject(upgradeData);

            // Can Sell
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Can Sell", GUILayout.Width(100));
            SerializedProperty canSellProp = so.FindProperty("canSell");
            EditorGUILayout.PropertyField(canSellProp, GUIContent.none, GUILayout.Width(20));

            if (canSellProp.boolValue)
            {
                EditorGUILayout.LabelField("Sell Value %", GUILayout.Width(80));
                SerializedProperty sellValueProp = so.FindProperty("sellValuePercent");
                EditorGUILayout.PropertyField(sellValueProp, GUIContent.none, GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Upgrade Options
            SerializedProperty upgradeOptionsProp = so.FindProperty("upgradeOptions");
            EditorGUILayout.LabelField("Upgrade Options:", EditorStyles.boldLabel);

            for (int i = 0; i < upgradeOptionsProp.arraySize; i++)
            {
                SerializedProperty option = upgradeOptionsProp.GetArrayElementAtIndex(i);
                DrawUpgradeOption(option, i, upgradeOptionsProp);
            }

            // Add new upgrade option button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Upgrade Option", GUILayout.Width(150)))
            {
                upgradeOptionsProp.InsertArrayElementAtIndex(upgradeOptionsProp.arraySize);
            }
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(upgradeData);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawUpgradeOption(SerializedProperty option, int index, SerializedProperty arrayProp)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Option {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60));

        // Remove button
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            arrayProp.DeleteArrayElementAtIndex(index);
            return;
        }
        EditorGUILayout.EndHorizontal();

        // Upgraded Tower
        SerializedProperty upgradedTowerProp = option.FindPropertyRelative("upgradedTower");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Upgrades To:", GUILayout.Width(100));
        EditorGUILayout.PropertyField(upgradedTowerProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        // Upgrade Cost
        SerializedProperty costProp = option.FindPropertyRelative("upgradeCost");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cost:", GUILayout.Width(100));
        EditorGUILayout.PropertyField(costProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        // Requirements (collapsible)
        SerializedProperty requirementsProp = option.FindPropertyRelative("requirements");
        requirementsProp.isExpanded = EditorGUILayout.Foldout(requirementsProp.isExpanded, "Requirements (Optional)");

        if (requirementsProp.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(requirementsProp, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private TowerUpgradeData FindUpgradeDataForTower(TowerSO tower)
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerUpgradeData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerUpgradeData data = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);

            if (data != null && data.baseTower == tower)
            {
                return data;
            }
        }
        return null;
    }

    private void CreateUpgradeDataForTower(TowerSO tower)
    {
        // Check if already exists
        TowerUpgradeData existing = FindUpgradeDataForTower(tower);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                $"TowerUpgradeData already exists for {tower.name}:\n{AssetDatabase.GetAssetPath(existing)}",
                "OK");
            Selection.activeObject = existing;
            return;
        }

        // Create new TowerUpgradeData
        TowerUpgradeData newData = ScriptableObject.CreateInstance<TowerUpgradeData>();
        newData.baseTower = tower;
        newData.canSell = true;
        newData.sellValuePercent = 75;

        // Save to same folder as tower
        string towerPath = AssetDatabase.GetAssetPath(tower);
        string folderPath = System.IO.Path.GetDirectoryName(towerPath);
        string assetPath = $"{folderPath}/{tower.name}_Upgrades.asset";

        AssetDatabase.CreateAsset(newData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = newData;
        EditorGUIUtility.PingObject(newData);

        Debug.Log($"Created TowerUpgradeData for {tower.name} at {assetPath}");
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
        // Save towers
        foreach (var tower in towers)
        {
            EditorUtility.SetDirty(tower);
        }

        // Save all TowerUpgradeData assets
        string[] guids = AssetDatabase.FindAssets("t:TowerUpgradeData");
        int upgradeDataCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerUpgradeData data = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);
            if (data != null)
            {
                EditorUtility.SetDirty(data);
                upgradeDataCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TowerDataEditor] Saved {towers.Count} towers and {upgradeDataCount} upgrade data assets.");
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
                EditorGUILayout.LabelField("–", GUILayout.Width(FieldWidth));
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
                EditorGUILayout.LabelField("–", GUILayout.Width(FieldWidth));
                continue;
            }

            T oldVal = getter(tower);
            T newVal = (T)EditorGUILayout.EnumPopup(oldVal, GUILayout.Width(FieldWidth));

            if (!oldVal.Equals(newVal))
            {
                Undo.RecordObject(tower, $"Edit {label}");
                setter(tower, newVal);
                EditorUtility.SetDirty(tower);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}