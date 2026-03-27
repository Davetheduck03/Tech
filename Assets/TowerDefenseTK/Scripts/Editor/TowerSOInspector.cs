using UnityEditor;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Custom inspector for TowerSO assets.
///
/// Sections shown depend on the selected TowerType and AOEType so the inspector
/// only ever shows fields that are actually used:
///
///   Turret    — basic stats, fire rate, range, targeting, status effect
///   AoE       — same + AOE subtype, radius, projectile speed
///              ↳ Turret_AOE — ProjectileConfig (arc height, fixed target)
///              ↳ Cone       — Cone Angle
///   Support   — range, status effect (debuff aura) and/or tower buff (ally aura)
///   Resource  — gold per second only (no combat fields)
/// </summary>
[CustomEditor(typeof(TowerSO))]
public class TowerSOInspector : Editor
{
    // ── Cached serialized properties ──────────────────────────────────────────

    // Basic Info
    SerializedProperty p_unitName, p_unitPrefab, p_icon;

    // Base Stats
    SerializedProperty p_health, p_damage, p_damageType, p_isTargetable, p_armor, p_defenseTypes;

    // Tower Stats
    SerializedProperty p_buildCost, p_previewPrefab;
    SerializedProperty p_towerType, p_fireRate, p_range;
    SerializedProperty p_towerTargetType, p_targetGroup;

    // AOE
    SerializedProperty p_aoeType, p_aoeRadius, p_projectileSpeed;
    SerializedProperty p_arcHeight, p_useFixedTarget; // sub-fields of projectileConfig struct
    SerializedProperty p_coneAngle;

    // Effects / specialisations
    SerializedProperty p_statusEffect;
    SerializedProperty p_towerBuff;
    SerializedProperty p_goldPerSecond;

    private void OnEnable()
    {
        // Basic Info
        p_unitName    = serializedObject.FindProperty("UnitName");
        p_unitPrefab  = serializedObject.FindProperty("UnitPrefab");
        p_icon        = serializedObject.FindProperty("Icon");

        // Base Stats
        p_health      = serializedObject.FindProperty("Health");
        p_damage      = serializedObject.FindProperty("damage");
        p_damageType  = serializedObject.FindProperty("damageType");
        p_isTargetable = serializedObject.FindProperty("isTargetable");
        p_armor       = serializedObject.FindProperty("armor");
        p_defenseTypes = serializedObject.FindProperty("defenseTypes");

        // Tower Stats
        p_buildCost       = serializedObject.FindProperty("buildCost");
        p_previewPrefab   = serializedObject.FindProperty("previewPrefab");
        p_towerType       = serializedObject.FindProperty("towerType");
        p_fireRate        = serializedObject.FindProperty("fireRate");
        p_range           = serializedObject.FindProperty("range");
        p_towerTargetType = serializedObject.FindProperty("towerTargetType");
        p_targetGroup     = serializedObject.FindProperty("targetGroup");

        // AOE
        p_aoeType        = serializedObject.FindProperty("AOEType");
        p_aoeRadius      = serializedObject.FindProperty("AOE_Radius");
        p_projectileSpeed = serializedObject.FindProperty("projectileSpeed");

        // projectileConfig is a struct — we drive its child properties directly.
        SerializedProperty configProp = serializedObject.FindProperty("projectileConfig");
        p_arcHeight      = configProp.FindPropertyRelative("arcHeight");
        p_useFixedTarget = configProp.FindPropertyRelative("useFixedTarget");

        p_coneAngle = serializedObject.FindProperty("coneAngle");

        // Specialisations
        p_statusEffect  = serializedObject.FindProperty("statusEffect");
        p_towerBuff     = serializedObject.FindProperty("towerBuff");
        p_goldPerSecond = serializedObject.FindProperty("goldPerSecond");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TowerSO tower = (TowerSO)target;
        TowerType type = (TowerType)p_towerType.enumValueIndex;

        // ── Basic Info ────────────────────────────────────────────────────────
        Section("Basic Info");
        EditorGUILayout.PropertyField(p_unitName);
        EditorGUILayout.PropertyField(p_unitPrefab);
        EditorGUILayout.PropertyField(p_icon);

        // ── Base Stats ────────────────────────────────────────────────────────
        Section("Base Stats");
        EditorGUILayout.PropertyField(p_health);
        EditorGUILayout.PropertyField(p_isTargetable);

        bool isCombat = type == TowerType.Turret || type == TowerType.AoE;
        if (isCombat)
        {
            EditorGUILayout.PropertyField(p_damage);
            EditorGUILayout.PropertyField(p_damageType);
            EditorGUILayout.PropertyField(p_armor,
                new GUIContent("Armor", "Flat armor value for this tower (damage reduction)."));
            EditorGUILayout.PropertyField(p_defenseTypes, true);
        }

        // ── Tower Stats ───────────────────────────────────────────────────────
        Section("Tower Settings");
        EditorGUILayout.PropertyField(p_buildCost);
        EditorGUILayout.PropertyField(p_previewPrefab);
        EditorGUILayout.PropertyField(p_towerType);

        // Re-read type after it may have just changed in the inspector
        type = (TowerType)p_towerType.enumValueIndex;

        // ── Combat fields (Turret / AoE / debuff Support) ─────────────────────
        bool showCombatFields = type != TowerType.Resource;
        if (showCombatFields)
        {
            EditorGUILayout.PropertyField(p_fireRate,
                new GUIContent("Fire Rate", "Shots or pulses per second."));
            EditorGUILayout.PropertyField(p_range,
                new GUIContent("Range", "Detection / aura radius in world units."));
        }

        // Targeting — not needed for Support buff towers or Resource
        bool showTargeting = type == TowerType.Turret || type == TowerType.AoE;
        if (showTargeting)
        {
            EditorGUILayout.PropertyField(p_towerTargetType,
                new GUIContent("Target Priority", "Which enemy to lock on to."));
            EditorGUILayout.PropertyField(p_targetGroup,
                new GUIContent("Target Group", "Ground, Air, or Both."));
        }

        // ── AoE section ───────────────────────────────────────────────────────
        if (type == TowerType.AoE)
        {
            Section("AoE Settings");
            EditorGUILayout.PropertyField(p_aoeType);

            AOEType aoeType = (AOEType)p_aoeType.enumValueIndex;

            // Turret_AOE fires a projectile — show projectile fields
            if (aoeType == AOEType.Turret_AOE)
            {
                EditorGUILayout.PropertyField(p_projectileSpeed,
                    new GUIContent("Projectile Speed", "World units per second."));
                EditorGUILayout.PropertyField(p_aoeRadius,
                    new GUIContent("AOE Radius", "Blast radius on impact in world units."));

                // ── ProjectileConfig ────────────────────────────────────────
                HelpSection("Projectile Config",
                    "Controls the flight behaviour of this tower's projectiles.");

                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(p_arcHeight,
                        new GUIContent("Arc Height",
                            "Peak height of the ballistic arc at the midpoint.\n" +
                            "0 = flat straight-line flight.\n" +
                            "3–5 = a satisfying mortar lob."));

                    EditorGUILayout.PropertyField(p_useFixedTarget,
                        new GUIContent("Fixed Target (Predictive)",
                            "When ON: locks the destination to the target's position at the moment of firing.\n" +
                            "The projectile keeps flying even if the target dies or moves.\n\n" +
                            "When OFF: steers toward the live target position every frame (no arc)."));
                }
            }

            // Cone — show angle only
            if (aoeType == AOEType.Cone)
            {
                EditorGUILayout.PropertyField(p_coneAngle,
                    new GUIContent("Cone Angle",
                        "Full opening angle in degrees.\n" +
                        "60 = 30° to each side of the weapon's forward direction."));
            }

            // Circle — no extra fields needed (range = pulse radius, fireRate = rate)
            if (aoeType == AOEType.Circle)
            {
                EditorGUILayout.HelpBox(
                    "Circle AOE: damages all enemies within Range at the tower's Fire Rate.\n" +
                    "No extra settings needed — tune Range and Fire Rate above.",
                    MessageType.Info);
            }
        }

        // ── Status Effect (Turret, AoE, Support-debuff) ───────────────────────
        bool showStatusEffect = type != TowerType.Resource;
        if (showStatusEffect)
        {
            Section("Status Effect");
            EditorGUILayout.PropertyField(p_statusEffect,
                new GUIContent("On-Hit / Aura Effect",
                    "Turret / AoE: applied to every enemy on hit.\n" +
                    "Support: pulsed to all enemies in range every 0.5 s.\n" +
                    "Leave empty for no effect."));
        }

        // ── Support — buff aura ───────────────────────────────────────────────
        if (type == TowerType.Support)
        {
            Section("Buff Tower");
            EditorGUILayout.PropertyField(p_towerBuff,
                new GUIContent("Tower Buff Preset",
                    "TowerBuffSO pulsed to allied towers in range every 0.5 s.\n" +
                    "Receiving towers must have a TowerBuffComponent attached.\n" +
                    "Leave empty if this tower only debuffs enemies (statusEffect above)."));

            if (p_towerBuff.objectReferenceValue == null && p_statusEffect.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "This Support tower has no effect assigned.\n" +
                    "Add a Status Effect (enemy debuff) or a Tower Buff (ally boost) — or both.",
                    MessageType.Warning);
            }
        }

        // ── Resource ──────────────────────────────────────────────────────────
        if (type == TowerType.Resource)
        {
            Section("Resource Tower");
            EditorGUILayout.PropertyField(p_goldPerSecond,
                new GUIContent("Gold Per Second",
                    "Passive income added to CurrencyManager every second.\n" +
                    "No enemies required — just place and earn."));

            if (p_goldPerSecond.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox(
                    "Gold Per Second is 0 — this miner won't generate any income.",
                    MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Draws a bold section header with a thin separator line above it.</summary>
    private static void Section(string title)
    {
        EditorGUILayout.Space(6);
        Rect r = GUILayoutUtility.GetRect(float.MaxValue, 1f);
        EditorGUI.DrawRect(r, new Color(0.28f, 0.28f, 0.28f));
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
    }

    /// <summary>Bold label + grey tooltip line — for sub-sections like ProjectileConfig.</summary>
    private static void HelpSection(string title, string subtitle)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
        EditorGUILayout.Space(2);
    }
}
