using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Editor window for creating and tweaking StatusEffectSO presets.
/// Open via  Tools → Status Effect Editor
/// </summary>
public class W_StatusEffectEditor : EditorWindow
{
    [MenuItem("Tools/Status Effect Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<W_StatusEffectEditor>("Status Effect Editor");
        window.minSize = new Vector2(580, 440);
        window.Show();
    }

    // ── State ────────────────────────────────────────────────────────────────

    private List<StatusEffectSO> effects      = new List<StatusEffectSO>();
    private StatusEffectSO       selected;
    private Vector2              listScroll;
    private Vector2              editScroll;

    // Create-new form
    private string          newName    = "New Status Effect";
    private StatusEffectType newType   = StatusEffectType.Slow;
    private string          saveFolder = "Assets/TowerDefenseTK/Data/StatusEffects";

    // Layout constants
    private const float ListWidth  = 210f;
    private const float LabelWidth = 150f;

    // Type colours — match the tints used in-game
    private static readonly Color SlowColor = new Color(0.45f, 0.65f, 1.00f);
    private static readonly Color DOTColor  = new Color(1.00f, 0.55f, 0.20f);
    private static readonly Color StunColor = new Color(1.00f, 0.90f, 0.20f);

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable() => Refresh();

    private void OnGUI()
    {
        DrawHeader();

        // Main two-panel body
        EditorGUILayout.BeginHorizontal();
        DrawListPanel();
        DrawVerticalLine();
        DrawEditPanel();
        EditorGUILayout.EndHorizontal();

        DrawHorizontalLine();
        EditorGUILayout.Space(4);
        DrawCreatePanel();
        EditorGUILayout.Space(6);
    }

    // ── Header ───────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Status Effect Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Create and tweak Slow, DOT and Stun presets", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(6);

        if (GUILayout.Button("Refresh", GUILayout.Height(22)))
            Refresh();

        EditorGUILayout.Space(4);
        DrawHorizontalLine();
        EditorGUILayout.Space(4);
    }

    // ── Left Panel — asset list ───────────────────────────────────────────────

    private void DrawListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField($"Effects  ({effects.Count})", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));

        if (effects.Count == 0)
        {
            EditorGUILayout.HelpBox("No StatusEffectSO assets found.\nUse the Create panel below.", MessageType.Info);
        }

        foreach (var e in effects)
        {
            if (e == null) continue;

            bool isSelected = e == selected;
            Color typeCol   = TypeColor(e.effectType);

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? typeCol : typeCol * 0.55f;

            GUIStyle btn = isSelected
                ? new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold }
                : new GUIStyle(EditorStyles.miniButton)    { alignment = TextAnchor.MiddleLeft };

            string label = $"  [{ShortType(e.effectType)}]  {e.effectName}";

            if (GUILayout.Button(label, btn, GUILayout.Height(22)))
            {
                selected = e;
                GUI.FocusControl(null);
                Repaint();
            }

            GUI.backgroundColor = prev;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ── Right Panel — edit selected ───────────────────────────────────────────

    private void DrawEditPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (selected == null)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("← Select an effect to edit it.", MessageType.Info);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        editScroll = EditorGUILayout.BeginScrollView(editScroll, GUILayout.ExpandHeight(true));

        // Coloured type badge header
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = TypeColor(selected.effectType) * 0.65f;
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(
            $"  {selected.effectType.ToString().ToUpper()} — {selected.effectName}",
            EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = prev;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        DrawGeneralFields();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // Type-specific section
        switch (selected.effectType)
        {
            case StatusEffectType.Slow: DrawSlowSection(); break;
            case StatusEffectType.DOT:  DrawDOTSection();  break;
            case StatusEffectType.Stun: DrawStunSection(); break;
        }

        EditorGUILayout.Space(10);
        DrawEditActions();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ── General fields (shared by all types) ─────────────────────────────────

    private void DrawGeneralFields()
    {
        EditorGUI.BeginChangeCheck();
        float savedLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;

        string newEffName = EditorGUILayout.TextField(
            new GUIContent("Effect Name", "Displayed in the inspector and editor."),
            selected.effectName);

        float newDur = EditorGUILayout.FloatField(
            new GUIContent("Duration (s)", "How many seconds the effect lasts on the enemy."),
            selected.duration);

        Color newTint = EditorGUILayout.ColorField(
            new GUIContent("Tint Color", "Enemy color while this effect is active."),
            selected.tintColor);

        // Type change — guard with a confirmation dialog
        StatusEffectType oldType = selected.effectType;
        StatusEffectType newTypeSel = (StatusEffectType)EditorGUILayout.EnumPopup(
            new GUIContent("Effect Type", "Changing type keeps old serialized values but they won't be used."),
            oldType);

        EditorGUIUtility.labelWidth = savedLabelWidth;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selected, "Edit Status Effect");

            if (newTypeSel != oldType)
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Change Effect Type?",
                    $"Changing type from {oldType} → {newTypeSel}.\n\n" +
                    "Type-specific settings for the previous type will be kept in the asset " +
                    "but will not be used unless you switch back.",
                    "Change", "Cancel");

                if (ok)
                {
                    selected.effectType = newTypeSel;
                    // Default the tint to the canonical colour for the new type
                    selected.tintColor = TypeColor(newTypeSel);
                    Refresh(); // re-sort list
                }
            }
            else
            {
                selected.effectType = newTypeSel;
            }

            selected.effectName = newEffName;
            selected.duration   = Mathf.Max(0.05f, newDur);
            selected.tintColor  = newTint;
            EditorUtility.SetDirty(selected);
        }
    }

    // ── Slow section ─────────────────────────────────────────────────────────

    private void DrawSlowSection()
    {
        EditorGUILayout.LabelField("Slow Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();
        float savedLW = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;

        float newMult = EditorGUILayout.Slider(
            new GUIContent("Speed Multiplier",
                "How fast the enemy moves while slowed.\n1.0 = full speed, 0.5 = half speed, 0.1 = nearly frozen."),
            selected.slowMultiplier, 0.05f, 0.95f);

        EditorGUIUtility.labelWidth = savedLW;

        // Read-only strength label
        EditorGUI.BeginDisabledGroup(true);
        int pct = Mathf.RoundToInt((1f - newMult) * 100f);
        EditorGUILayout.TextField("Slow Strength", $"{pct}% speed reduction");
        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selected, "Edit Slow Multiplier");
            selected.slowMultiplier = newMult;
            EditorUtility.SetDirty(selected);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Stacking rule: only the strongest active slow is applied.\n" +
            "A weaker slow from a second tower will only refresh the timer.",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    // ── DOT section ──────────────────────────────────────────────────────────

    private void DrawDOTSection()
    {
        EditorGUILayout.LabelField("DOT Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();
        float savedLW = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;

        float newDPS = EditorGUILayout.FloatField(
            new GUIContent("Damage Per Second",
                "Raw DPS before the DamageTable multiplier is applied.\nArmour and resistances affect the final value."),
            selected.damagePerSecond);

        DamageType newDmgType = (DamageType)EditorGUILayout.ObjectField(
            new GUIContent("Damage Type",
                "Which DamageType SO to use for DamageTable lookups.\nLeave empty to skip the table (raw damage)."),
            selected.dotDamageType, typeof(DamageType), false);

        EditorGUIUtility.labelWidth = savedLW;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selected, "Edit DOT Settings");
            selected.damagePerSecond = Mathf.Max(0f, newDPS);
            selected.dotDamageType   = newDmgType;
            EditorUtility.SetDirty(selected);
        }

        // Read-only total damage preview
        if (selected.damagePerSecond > 0f && selected.duration > 0f)
        {
            EditorGUILayout.Space(4);
            float total = selected.damagePerSecond * selected.duration;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(
                new GUIContent("Total Damage (pre-armour)", "DPS × Duration, before DamageTable."),
                $"{total:F1}");
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Stacking rule: one DOT instance per tower. Re-applying the same effect resets the timer " +
            "rather than stacking damage.\nKill credit goes to whichever tower applied the DOT.",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    // ── Stun section ─────────────────────────────────────────────────────────

    private void DrawStunSection()
    {
        EditorGUILayout.LabelField("Stun Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox(
            "Stun freezes the enemy completely for the Duration set above.\n" +
            "No additional settings are needed.\n\n" +
            "Stacking rule: only one stun is active at a time. A longer incoming stun replaces a shorter one.",
            MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // ── Edit action buttons ───────────────────────────────────────────────────

    private void DrawEditActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Height(28)))
        {
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatusEffectEditor] Saved '{selected.effectName}'.");
        }

        if (GUILayout.Button("Ping in Project", GUILayout.Height(28)))
            EditorGUIUtility.PingObject(selected);

        GUILayout.FlexibleSpace();

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.38f, 0.38f);
        if (GUILayout.Button("Delete Asset", GUILayout.Height(28), GUILayout.Width(100)))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Delete Status Effect",
                $"Permanently delete '{selected.effectName}'?\n\nThis cannot be undone.",
                "Delete", "Cancel");

            if (confirm)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                selected = null;
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
                Refresh();
            }
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndHorizontal();
    }

    // ── Create-new panel ─────────────────────────────────────────────────────

    private void DrawCreatePanel()
    {
        EditorGUILayout.LabelField("Create New Status Effect", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        float savedLW = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;

        newName = EditorGUILayout.TextField(
            new GUIContent("Name", "File name of the new asset."),
            newName);

        newType = (StatusEffectType)EditorGUILayout.EnumPopup(
            new GUIContent("Type", "Sets the effect type and sensible defaults."),
            newType);

        EditorGUILayout.BeginHorizontal();
        saveFolder = EditorGUILayout.TextField(
            new GUIContent("Save Folder", "Asset folder path relative to the project root."),
            saveFolder);

        if (GUILayout.Button("Browse", GUILayout.Width(60), GUILayout.Height(18)))
        {
            string chosen = EditorUtility.OpenFolderPanel("Choose Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(chosen) && chosen.StartsWith(Application.dataPath))
                saveFolder = "Assets" + chosen.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = savedLW;

        EditorGUILayout.Space(4);

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.48f, 0.85f, 0.48f);
        if (GUILayout.Button("Create Asset", GUILayout.Height(30)))
            CreateAsset();
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndVertical();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        effects.Clear();

        string[] guids = AssetDatabase.FindAssets("t:StatusEffectSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var e = AssetDatabase.LoadAssetAtPath<StatusEffectSO>(path);
            if (e != null) effects.Add(e);
        }

        // Sort by type, then alphabetically by name
        effects.Sort((a, b) =>
        {
            int tc = a.effectType.CompareTo(b.effectType);
            return tc != 0 ? tc : string.Compare(a.effectName, b.effectName,
                                                  System.StringComparison.OrdinalIgnoreCase);
        });

        Repaint();
    }

    private void CreateAsset()
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name.", "OK");
            return;
        }

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        StatusEffectSO asset = ScriptableObject.CreateInstance<StatusEffectSO>();
        asset.effectName  = newName;
        asset.effectType  = newType;
        asset.duration    = 2f;
        asset.tintColor   = TypeColor(newType);

        // Sensible defaults per type
        switch (newType)
        {
            case StatusEffectType.Slow:
                asset.slowMultiplier  = 0.5f;
                break;
            case StatusEffectType.DOT:
                asset.damagePerSecond = 5f;
                break;
        }

        string safeName  = newName.Replace(" ", "_");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{safeName}.asset");

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Refresh();
        selected = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[StatusEffectEditor] Created '{asset.effectName}' at {assetPath}");
    }

    private static Color TypeColor(StatusEffectType t) => t switch
    {
        StatusEffectType.Slow => SlowColor,
        StatusEffectType.DOT  => DOTColor,
        StatusEffectType.Stun => StunColor,
        _                     => Color.white
    };

    private static string ShortType(StatusEffectType t) => t switch
    {
        StatusEffectType.Slow => "SLW",
        StatusEffectType.DOT  => "DOT",
        StatusEffectType.Stun => "STN",
        _                     => "???"
    };

    private static void DrawHorizontalLine()
    {
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(float.MaxValue, 1f), new Color(0.28f, 0.28f, 0.28f));
    }

    private static void DrawVerticalLine()
    {
        Rect r = GUILayoutUtility.GetRect(1f, float.MaxValue, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(r, new Color(0.28f, 0.28f, 0.28f));
        GUILayout.Space(4f);
    }
}
