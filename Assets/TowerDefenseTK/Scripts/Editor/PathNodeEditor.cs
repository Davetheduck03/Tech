using UnityEngine;
using UnityEditor;
using TowerDefenseTK;

/// <summary>
/// Custom Inspector + Scene-view gizmos for individual PathNode components.
///
/// Inspector  — shows a colour-coded tile-type badge, walkability status,
///              and a quick-change dropdown so you can edit the tile type
///              without entering Play mode.
///
/// Scene view — when a PathNode is selected it draws a coloured filled disc
///              at the node's position plus a floating type label.
///              All PathNodes also draw a small icon while unselected so you
///              can spot their types at a glance in the scene.
/// </summary>
[CustomEditor(typeof(PathNode))]
public class PathNodeEditor : Editor
{
    // ── Colours (shared with PathNodeGeneratorInspector) ─────────────────────
    private static Color GetTileColor(TileType type) =>
        PathNodeGeneratorInspector.GetTileColor(type);

    // ── Inspector ─────────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        PathNode node = (PathNode)target;

        serializedObject.Update();

        // ── Tile type badge ───────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        DrawTileBadge(node.TileType);
        EditorGUILayout.Space(4);

        // ── Walkability strip ─────────────────────────────────────────────────
        Color stripCol = node.isWalkable
            ? new Color(0.25f, 0.75f, 0.25f, 0.35f)
            : new Color(0.85f, 0.25f, 0.25f, 0.35f);

        Rect stripRect = EditorGUILayout.GetControlRect(false, 18f);
        EditorGUI.DrawRect(stripRect, stripCol);
        EditorGUI.LabelField(stripRect,
            node.isWalkable ? "  ✔  Walkable" : "  ✖  Not walkable",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        // ── Tower status ──────────────────────────────────────────────────────
        if (node.HasTower)
        {
            Rect towerRect = EditorGUILayout.GetControlRect(false, 18f);
            EditorGUI.DrawRect(towerRect, new Color(0.8f, 0.5f, 0.1f, 0.35f));
            EditorGUI.LabelField(towerRect, "  🗼  Tower present", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
        }

        // ── Default fields ────────────────────────────────────────────────────
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();

        // ── Quick tile-type changer (edit mode only) ──────────────────────────
        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Quick Edit (Edit Mode)", EditorStyles.boldLabel);

            TileType newType = (TileType)EditorGUILayout.EnumPopup("Tile Type", node.TileType);
            if (newType != node.TileType)
            {
                Undo.RecordObject(node, "Change Tile Type");
                node.SetTileType(newType);
                EditorUtility.SetDirty(node);
            }
        }
    }

    private static void DrawTileBadge(TileType type)
    {
        Color col  = GetTileColor(type);
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = col;

        GUIStyle style = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        GUILayout.Box($"  {type}  ", style,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(28f));

        GUI.backgroundColor = prev;
    }

    // ── Scene-view ────────────────────────────────────────────────────────────

    private void OnSceneGUI()
    {
        PathNode node = (PathNode)target;
        DrawNodeGizmo(node, selected: true);
    }

    /// <summary>
    /// Draw gizmos for ALL PathNodes in the scene (not just the selected one)
    /// so the map is always colour-coded in the Scene view.
    /// Called by Unity via [DrawGizmo] attribute.
    /// </summary>
    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
    private static void DrawGizmoForPathNode(PathNode node, GizmoType gizmoType)
    {
        DrawNodeGizmo(node, selected: false);
    }

    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    private static void DrawGizmoForSelectedPathNode(PathNode node, GizmoType gizmoType)
    {
        // Selected nodes are drawn by OnSceneGUI with full detail; skip here
        // to avoid double-drawing.
    }

    // ── Shared drawing logic ──────────────────────────────────────────────────

    private static void DrawNodeGizmo(PathNode node, bool selected)
    {
        Vector3 pos = node.transform.position;
        TileType type = node.TileType;
        Color col = GetTileColor(type);

        // Camera-distance fade — don't clutter distant nodes
        Camera sceneCamera = SceneView.currentDrawingSceneView?.camera;
        float dist = sceneCamera != null
            ? Vector3.Distance(sceneCamera.transform.position, pos)
            : 10f;

        float fadeStart = 25f;
        float fadeEnd   = 60f;
        float alpha     = 1f - Mathf.Clamp01((dist - fadeStart) / (fadeEnd - fadeStart));
        if (alpha <= 0f) return;

        float discRadius = selected ? 0.38f : 0.25f;

        // Filled disc (flat, lying on the XZ plane)
        Handles.color = new Color(col.r, col.g, col.b, (selected ? 0.85f : 0.55f) * alpha);
        Handles.DrawSolidDisc(pos, Vector3.up, discRadius);

        // Outline ring
        Handles.color = new Color(
            Mathf.Clamp01(col.r - 0.2f),
            Mathf.Clamp01(col.g - 0.2f),
            Mathf.Clamp01(col.b - 0.2f),
            0.9f * alpha);
        Handles.DrawWireDisc(pos, Vector3.up, discRadius);

        // Tower indicator — small orange ring
        if (node.HasTower)
        {
            Handles.color = new Color(1f, 0.55f, 0.1f, 0.9f * alpha);
            Handles.DrawWireDisc(pos, Vector3.up, discRadius + 0.08f);
        }

        // Non-walkable cross
        if (!node.isWalkable && alpha > 0.3f)
        {
            Handles.color = new Color(1f, 0.15f, 0.15f, 0.85f * alpha);
            float r = discRadius * 0.6f;
            Handles.DrawLine(pos + new Vector3(-r, 0, -r), pos + new Vector3(r, 0, r));
            Handles.DrawLine(pos + new Vector3(-r, 0,  r), pos + new Vector3(r, 0, -r));
        }

        // Label — only when selected or close enough
        bool showLabel = selected || dist < 18f;
        if (showLabel && alpha > 0.2f)
        {
            string label = type switch
            {
                TileType.Spawn     => "SPAWN",
                TileType.Exit      => "EXIT",
                TileType.Hybrid    => "HYB",
                TileType.Blocked   => "BLK",
                TileType.Buildable => "BLD",
                TileType.Path      => "PATH",
                _                  => ""
            };

            if (!string.IsNullOrEmpty(label))
            {
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal    = { textColor = Color.white },
                    fontSize  = selected ? 11 : 9,
                    alignment = TextAnchor.MiddleCenter
                };

                Handles.Label(pos + Vector3.up * (discRadius + 0.15f), label, style);
            }
        }
    }
}
