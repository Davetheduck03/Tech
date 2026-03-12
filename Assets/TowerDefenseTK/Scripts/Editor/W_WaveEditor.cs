using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TowerDefenseTK;

public class W_WaveEditor : EditorWindow
{
    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<W_WaveEditor>("Wave Editor");
        window.minSize = new Vector2(420, 540);
        window.Show();
    }

    private List<MapData> maps         = new List<MapData>();
    private int           selectedMap  = 0;
    private MapData       currentMap   => maps.Count > 0 && selectedMap < maps.Count ? maps[selectedMap] : null;
    private Vector2       scrollPos;
    private int           selectedSpawner = 0;

    // Per-wave foldout state [spawnerIndex][waveIndex]
    private bool[][] waveFoldouts = new bool[0][];

    private void OnEnable() => RefreshMaps();

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        DrawHeader();
        EditorGUILayout.Space(5);

        DrawMapSelector();
        EditorGUILayout.Space(10);

        if (currentMap == null)
        {
            EditorGUILayout.HelpBox("No MapData assets found in the project.\n\nCreate one via the Grid Map Editor.", MessageType.Info);
            return;
        }

        currentMap.SyncSpawnerWaves();
        SyncFoldouts();

        if (currentMap.spawnPoints.Count == 0)
        {
            EditorGUILayout.HelpBox("This map has no spawn points. Add Spawn tiles in the Grid Map Editor first.", MessageType.Warning);
            return;
        }

        DrawSpawnerTabs();
        EditorGUILayout.Space(10);

        DrawWaveList();
        EditorGUILayout.Space(10);

        DrawActions();
    }

    // ─── Header ───────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Wave Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Configure enemy waves per spawn point", EditorStyles.centeredGreyMiniLabel);
    }

    // ─── Map Selector ─────────────────────────────────────────────────────────

    private void DrawMapSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Maps ({maps.Count})", EditorStyles.boldLabel, GUILayout.Width(80));

        if (GUILayout.Button("Refresh", GUILayout.Width(65), GUILayout.Height(18)))
        {
            RefreshMaps();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (maps.Count == 0) return;

        // One tab per discovered MapData asset
        string[] mapNames = new string[maps.Count];
        for (int i = 0; i < maps.Count; i++)
            mapNames[i] = maps[i] != null ? maps[i].name : "(null)";

        int newSel = GUILayout.Toolbar(selectedMap, mapNames, GUILayout.Height(24));
        if (newSel != selectedMap)
        {
            selectedMap     = newSel;
            selectedSpawner = 0;
            waveFoldouts    = new bool[0][];
            Repaint();
        }
    }

    private void RefreshMaps()
    {
        maps.Clear();
        string[] guids = AssetDatabase.FindAssets("t:MapData");
        foreach (string guid in guids)
        {
            MapData m = AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null) maps.Add(m);
        }

        // Keep selection in bounds after refresh
        selectedMap = Mathf.Clamp(selectedMap, 0, Mathf.Max(0, maps.Count - 1));
        selectedSpawner = 0;
        waveFoldouts    = new bool[0][];
        Repaint();
    }

    // ─── Spawner Tabs ─────────────────────────────────────────────────────────

    private void DrawSpawnerTabs()
    {
        int count = currentMap.spawnPoints.Count;
        string[] labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            Vector2Int pt = currentMap.spawnPoints[i];
            int waves = currentMap.spawnerWaves[i].waves.Count;
            labels[i] = $"Spawner {i + 1}  ({pt.x},{pt.y})  |  {waves}w";
        }

        selectedSpawner = Mathf.Clamp(selectedSpawner, 0, count - 1);
        selectedSpawner = GUILayout.Toolbar(selectedSpawner, labels, GUILayout.Height(25));
    }

    // ─── Wave List ────────────────────────────────────────────────────────────

    private void DrawWaveList()
    {
        SpawnerWaveData data = currentMap.spawnerWaves[selectedSpawner];
        Vector2Int pt        = currentMap.spawnPoints[selectedSpawner];

        EditorGUILayout.LabelField($"Spawner {selectedSpawner + 1}  —  Grid ({pt.x}, {pt.y})", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{data.waves.Count} wave(s)  —  last wave repeats when exhausted", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        if (data.waves.Count == 0)
            EditorGUILayout.HelpBox("No waves configured. Add at least one wave.", MessageType.Warning);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < data.waves.Count; i++)
            DrawWaveEntry(data, i);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // ── Add / Clear ──────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Add Wave", GUILayout.Height(25)))
        {
            Undo.RecordObject(currentMap, "Add Wave");
            WaveConfig newWave = new WaveConfig();
            newWave.waveName = $"Wave {data.waves.Count + 1}";

            if (data.waves.Count > 0)
            {
                WaveConfig prev       = data.waves[data.waves.Count - 1];
                newWave.enemyPoolName = prev.enemyPoolName;
                newWave.enemyCount    = prev.enemyCount + 2;
                newWave.spawnInterval = prev.spawnInterval;
                newWave.cooldownAfter = prev.cooldownAfter;
            }

            data.waves.Add(newWave);
            SyncFoldouts();
            EditorUtility.SetDirty(currentMap);
        }

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Clear All", GUILayout.Height(25), GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Clear Waves",
                $"Remove all waves from Spawner {selectedSpawner + 1}?", "Clear", "Cancel"))
            {
                Undo.RecordObject(currentMap, "Clear Waves");
                data.waves.Clear();
                SyncFoldouts();
                EditorUtility.SetDirty(currentMap);
            }
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveEntry(SpawnerWaveData data, int i)
    {
        bool[] foldouts = waveFoldouts[selectedSpawner];
        WaveConfig wave = data.waves[i];
        string header   = $"Wave {i + 1}  —  {wave.waveName}  ({wave.enemyCount} enemies)";

        EditorGUILayout.BeginVertical("box");

        // ── Row: foldout + reorder + duplicate + delete ──────────────────
        EditorGUILayout.BeginHorizontal();

        foldouts[i] = EditorGUILayout.Foldout(foldouts[i], header, true, EditorStyles.foldoutHeader);

        // Move Up
        EditorGUI.BeginDisabledGroup(i == 0);
        if (GUILayout.Button("^", GUILayout.Width(22), GUILayout.Height(18)))
        {
            Undo.RecordObject(currentMap, "Move Wave Up");
            WaveConfig tmp  = data.waves[i];
            data.waves[i]   = data.waves[i - 1];
            data.waves[i-1] = tmp;
            bool ftmp       = foldouts[i];
            foldouts[i]     = foldouts[i - 1];
            foldouts[i - 1] = ftmp;
            EditorUtility.SetDirty(currentMap);
        }
        EditorGUI.EndDisabledGroup();

        // Move Down
        EditorGUI.BeginDisabledGroup(i == data.waves.Count - 1);
        if (GUILayout.Button("v", GUILayout.Width(22), GUILayout.Height(18)))
        {
            Undo.RecordObject(currentMap, "Move Wave Down");
            WaveConfig tmp  = data.waves[i];
            data.waves[i]   = data.waves[i + 1];
            data.waves[i+1] = tmp;
            bool ftmp       = foldouts[i];
            foldouts[i]     = foldouts[i + 1];
            foldouts[i + 1] = ftmp;
            EditorUtility.SetDirty(currentMap);
        }
        EditorGUI.EndDisabledGroup();

        // Duplicate
        if (GUILayout.Button("D", GUILayout.Width(22), GUILayout.Height(18)))
        {
            Undo.RecordObject(currentMap, "Duplicate Wave");
            WaveConfig copy = new WaveConfig
            {
                waveName      = wave.waveName + " (Copy)",
                enemyPoolName = wave.enemyPoolName,
                enemyCount    = wave.enemyCount,
                spawnInterval = wave.spawnInterval,
                cooldownAfter = wave.cooldownAfter
            };
            data.waves.Insert(i + 1, copy);
            SyncFoldouts();
            EditorUtility.SetDirty(currentMap);
        }

        // Delete
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
        {
            Undo.RecordObject(currentMap, "Delete Wave");
            data.waves.RemoveAt(i);
            SyncFoldouts();
            EditorUtility.SetDirty(currentMap);
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndHorizontal();

        // ── Body ─────────────────────────────────────────────────────────
        if (i < foldouts.Length && foldouts[i])
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            string newName  = EditorGUILayout.TextField  ("Name",               wave.waveName);
            string newPool  = EditorGUILayout.TextField  ("Enemy Pool",         wave.enemyPoolName);
            int    newCount = EditorGUILayout.IntField   ("Enemy Count",        wave.enemyCount);
            float  newInt   = EditorGUILayout.FloatField ("Spawn Interval (s)", wave.spawnInterval);
            float  newCool  = EditorGUILayout.FloatField ("Cooldown After (s)", wave.cooldownAfter);

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMap, "Edit Wave");
                wave.waveName      = newName;
                wave.enemyPoolName = newPool;
                wave.enemyCount    = Mathf.Max(1, newCount);
                wave.spawnInterval = Mathf.Max(0f, newInt);
                wave.cooldownAfter = Mathf.Max(0f, newCool);
                EditorUtility.SetDirty(currentMap);
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ─── Actions ──────────────────────────────────────────────────────────────

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Asset", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(currentMap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WaveEditor] Saved wave data for '{currentMap.name}'.");
        }

        if (GUILayout.Button("Open Grid Map Editor", GUILayout.Height(30)))
        {
            W_GridMapEditor.ShowWindow();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            $"{currentMap.spawnPoints.Count} spawner(s)  |  Map: {currentMap.name}",
            EditorStyles.miniLabel);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SyncFoldouts()
    {
        if (currentMap == null) return;

        int spawnerCount = currentMap.spawnerWaves.Count;

        if (waveFoldouts.Length != spawnerCount)
        {
            bool[][] updated = new bool[spawnerCount][];
            for (int s = 0; s < Mathf.Min(waveFoldouts.Length, spawnerCount); s++)
                updated[s] = waveFoldouts[s];
            for (int s = waveFoldouts.Length; s < spawnerCount; s++)
                updated[s] = new bool[0];
            waveFoldouts = updated;
        }

        for (int s = 0; s < spawnerCount; s++)
        {
            int waveCount = currentMap.spawnerWaves[s].waves.Count;
            bool[] cur    = waveFoldouts[s] ?? new bool[0];

            if (cur.Length != waveCount)
            {
                bool[] next = new bool[waveCount];
                for (int w = 0; w < Mathf.Min(cur.Length, waveCount); w++)
                    next[w] = cur[w];
                for (int w = cur.Length; w < waveCount; w++)
                    next[w] = true;
                waveFoldouts[s] = next;
            }
        }
    }
}
