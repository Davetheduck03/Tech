using UnityEditor;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Custom inspector for EnemySO assets.
///
/// Mirrors the TowerSOInspector pattern: sections are shown or hidden based on
/// what is actually relevant for the enemy's configuration.
///
///   Core stats     — Health, Speed, Armor, gold reward, damage type
///   Enemy Settings — isFlying, damageToBase
///   Tower Combat   — only when canAttackTowers is true: attackRange, attackRate
///   Custom Behavior — EnemyBehaviorSO override (runs on top of standard movement)
/// </summary>
[CustomEditor(typeof(EnemySO))]
public class EnemySOInspector : Editor
{
    // ── Cached serialized properties ──────────────────────────────────────────

    // Basic Info (from UnitSO)
    SerializedProperty p_unitName, p_unitPrefab, p_icon;

    // Base Stats (from UnitSO)
    SerializedProperty p_health, p_speed, p_damage, p_damageType;
    SerializedProperty p_armor, p_defenseTypes, p_goldReward, p_isTargetable;

    // Enemy-specific
    SerializedProperty p_isFlying;
    SerializedProperty p_damageToBase;

    // Tower Combat
    SerializedProperty p_canAttackTowers, p_attackRange, p_attackRate;

    // Custom Behaviour
    SerializedProperty p_customBehavior;

    private void OnEnable()
    {
        // Basic Info
        p_unitName   = serializedObject.FindProperty("UnitName");
        p_unitPrefab = serializedObject.FindProperty("UnitPrefab");
        p_icon       = serializedObject.FindProperty("Icon");

        // Base Stats
        p_health       = serializedObject.FindProperty("Health");
        p_speed        = serializedObject.FindProperty("Speed");
        p_damage       = serializedObject.FindProperty("damage");
        p_damageType   = serializedObject.FindProperty("damageType");
        p_armor        = serializedObject.FindProperty("armor");
        p_defenseTypes = serializedObject.FindProperty("defenseTypes");
        p_goldReward   = serializedObject.FindProperty("goldReward");
        p_isTargetable = serializedObject.FindProperty("isTargetable");

        // Enemy settings
        p_isFlying     = serializedObject.FindProperty("isFlying");
        p_damageToBase = serializedObject.FindProperty("damageToBase");

        // Tower combat
        p_canAttackTowers = serializedObject.FindProperty("canAttackTowers");
        p_attackRange     = serializedObject.FindProperty("attackRange");
        p_attackRate      = serializedObject.FindProperty("attackRate");

        // Custom behaviour
        p_customBehavior = serializedObject.FindProperty("customBehavior");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Basic Info ────────────────────────────────────────────────────────
        Section("Basic Info");
        EditorGUILayout.PropertyField(p_unitName);
        EditorGUILayout.PropertyField(p_unitPrefab);
        EditorGUILayout.PropertyField(p_icon);

        // ── Base Stats ────────────────────────────────────────────────────────
        Section("Base Stats");
        EditorGUILayout.PropertyField(p_health,
            new GUIContent("Health", "Max HP at spawn."));
        EditorGUILayout.PropertyField(p_speed,
            new GUIContent("Speed", "Base movement speed in world units per second."));
        EditorGUILayout.PropertyField(p_goldReward,
            new GUIContent("Gold Reward", "Gold awarded to the player on death."));
        EditorGUILayout.PropertyField(p_isTargetable,
            new GUIContent("Is Targetable", "When false, towers cannot lock onto this enemy."));

        EditorGUILayout.Space(2);

        EditorGUILayout.PropertyField(p_damage,
            new GUIContent("Damage (vs Towers)", "Damage dealt to a tower per hit when canAttackTowers is enabled."));
        EditorGUILayout.PropertyField(p_damageType,
            new GUIContent("Damage Type", "Physical / Magic / True — used with tower armor values."));
        EditorGUILayout.PropertyField(p_armor,
            new GUIContent("Armor", "Flat damage reduction applied to incoming tower attacks."));
        EditorGUILayout.PropertyField(p_defenseTypes, true);

        // ── Enemy Settings ────────────────────────────────────────────────────
        Section("Enemy Settings");

        EditorGUILayout.PropertyField(p_isFlying,
            new GUIContent("Is Flying",
                "Flying enemies are ignored by towers with Target Group = Ground.\n" +
                "Set Target Group to Air or Both on towers that should hit flying units."));

        EditorGUILayout.PropertyField(p_damageToBase,
            new GUIContent("Damage to Base",
                "Lives (or HP) deducted from the player when this enemy reaches the exit."));

        if (p_damageToBase.intValue <= 0)
        {
            EditorGUILayout.HelpBox(
                "Damage to Base is 0 — this enemy won't hurt the player on exit.",
                MessageType.Warning);
        }

        // ── Tower Combat ──────────────────────────────────────────────────────
        Section("Tower Combat");

        EditorGUILayout.PropertyField(p_canAttackTowers,
            new GUIContent("Can Attack Towers",
                "When enabled, this enemy will attempt to attack towers it walks near.\n" +
                "Requires an EnemyWeapon component on the prefab."));

        if (p_canAttackTowers.boolValue)
        {
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(p_attackRange,
                    new GUIContent("Attack Range",
                        "World-unit radius in which the enemy can detect and target towers."));
                EditorGUILayout.PropertyField(p_attackRate,
                    new GUIContent("Attack Rate",
                        "Attacks per second against towers."));

                if (p_attackRange.floatValue <= 0f)
                    EditorGUILayout.HelpBox("Attack Range is 0 — this enemy will never detect towers.", MessageType.Warning);

                if (p_attackRate.floatValue <= 0f)
                    EditorGUILayout.HelpBox("Attack Rate is 0 — this enemy will never actually fire.", MessageType.Warning);

                // Remind user about the required component
                EnemySO enemy = (EnemySO)target;
                if (enemy.UnitPrefab != null)
                {
                    bool hasWeapon = enemy.UnitPrefab.GetComponentInChildren<EnemyWeapon>() != null;
                    if (!hasWeapon)
                    {
                        EditorGUILayout.HelpBox(
                            "No EnemyWeapon component found on the prefab.\n" +
                            "canAttackTowers requires an EnemyWeapon child.",
                            MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Assign a Unit Prefab to verify EnemyWeapon is present.",
                        MessageType.Info);
                }
            }
        }

        // ── Custom Behaviour ──────────────────────────────────────────────────
        Section("Custom Behaviour");

        EditorGUILayout.PropertyField(p_customBehavior,
            new GUIContent("Custom Behavior",
                "Assign an EnemyBehaviorSO subclass to add fully custom per-frame logic.\n\n" +
                "Standard movement continues to run — the behavior's Tick() is called\n" +
                "on top every frame.  Use it for enrage phases, burst attacks, spawning\n" +
                "minions on death, etc.\n\n" +
                "Leave empty for default behavior."));

        if (p_customBehavior.objectReferenceValue != null)
        {
            EditorGUILayout.HelpBox(
                "Custom Behavior is active.\n" +
                "Tick() runs every frame alongside standard movement.\n" +
                "Use OnDeath() / OnReachExit() / OnSpawned() for lifecycle hooks.",
                MessageType.Info);

            // Inline editor for the assigned behavior SO
            EditorGUILayout.Space(4);
            HelpSection("Behavior Properties",
                "Fields on the assigned EnemyBehaviorSO asset:");

            Editor behaviourEditor = CreateEditor(p_customBehavior.objectReferenceValue);
            behaviourEditor.OnInspectorGUI();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Style helpers (match TowerSOInspector) ────────────────────────────────

    private static void Section(string title)
    {
        EditorGUILayout.Space(6);
        Rect r = GUILayoutUtility.GetRect(float.MaxValue, 1f);
        EditorGUI.DrawRect(r, new Color(0.28f, 0.28f, 0.28f));
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
    }

    private static void HelpSection(string title, string subtitle)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
        EditorGUILayout.Space(2);
    }
}
