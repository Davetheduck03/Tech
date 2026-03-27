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
        window.minSize = new Vector2(480, 540);
        window.Show();
    }

    private List<MapData> maps         = new List<MapData>();
    private int           selectedMap  = 0;
    private MapData       currentMap   => maps.Count > 0 && selectedMap < maps.Count ? maps[selectedMap] : null;
    private Vector2       scrollPos;
    private int           selectedSpawner = 0;

    // Per-wave foldout state [spawnerIndex][waveIndex]
    private bool[][] waveFoldouts = new bool[0][];

    // View mode
    private enum ViewMode { Waves, Timeline }
    private ViewMode viewMode = ViewMode.Waves;

    // Timeline selection
    private int timelineSelectedWave = -1;

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
        EditorGUILayout.Space(6);

        // ── View mode toggle ─────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Toggle(viewMode == ViewMode.Waves,    "Waves",    EditorStyles.miniButtonLeft,  GUILayout.Width(80)))
            viewMode = ViewMode.Waves;
        if (GUILayout.Toggle(viewMode == ViewMode.Timeline, "Timeline", EditorStyles.miniButtonRight, GUILayout.Width(80)))
            viewMode = ViewMode.Timeline;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);

        if (viewMode == ViewMode.Waves)
        {
            DrawWaveList();
            EditorGUILayout.Space(10);
            DrawActions();
        }
        else
        {
            DrawTimeline();
            EditorGUILayout.Space(10);
            DrawActions();
        }
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
            selectedMap           = newSel;
            selectedSpawner       = 0;
            waveFoldouts          = new bool[0][];
            timelineSelectedWave  = -1;
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
        selectedMap           = Mathf.Clamp(selectedMap, 0, Mathf.Max(0, maps.Count - 1));
        selectedSpawner       = 0;
        waveFoldouts          = new bool[0][];
        timelineSelectedWave  = -1;
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
        int newSpawner  = GUILayout.Toolbar(selectedSpawner, labels, GUILayout.Height(25));
        if (newSpawner != selectedSpawner)
        {
            selectedSpawner      = newSpawner;
            timelineSelectedWave = -1;
        }
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

    // ─── Timeline ─────────────────────────────────────────────────────────────

    private void DrawTimeline()
    {
        SpawnerWaveData data = currentMap.spawnerWaves[selectedSpawner];

        if (data.waves.Count == 0)
        {
            EditorGUILayout.HelpBox("No waves to display. Switch to Waves view and add some waves first.", MessageType.Info);
            return;
        }

        // ── Pre-compute timing ─────────────────────────────────────────────
        float[] spawnDurations = new float[data.waves.Count];
        float[] startTimes     = new float[data.waves.Count];

        float cursor = 0f;
        for (int i = 0; i < data.waves.Count; i++)
        {
            WaveConfig w      = data.waves[i];
            spawnDurations[i] = w.spawnInterval * Mathf.Max(0, w.enemyCount - 1);
            startTimes[i]     = cursor;
            cursor           += spawnDurations[i] + w.cooldownAfter;
        }
        float totalSpan = Mathf.Max(cursor, 1f);

        // ── Header ────────────────────────────────────────────────────────
        EditorGUILayout.LabelField(
            $"Spawner {selectedSpawner + 1}  —  Timeline  ({totalSpan:0.0}s total)",
            EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Colored = spawn phase   ▪ Grey = cooldown   Click a row to edit",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        // ── Layout constants ──────────────────────────────────────────────
        const float LabelWidth = 54f;
        const float RowHeight  = 24f;
        const float BarPad     = 3f;
        const float MinBarW    = 3f;
        const int   TickCount  = 8;

        float windowW   = EditorGUIUtility.currentViewWidth;
        float availBarW = windowW - LabelWidth - 28f; // 28 = scrollbar + padding

        // ── Scrollable rows ───────────────────────────────────────────────
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Time axis
        {
            Rect axisRect = GUILayoutUtility.GetRect(windowW, 18f);
            DrawTimeAxis(axisRect, LabelWidth, availBarW, totalSpan, TickCount);
        }

        // Separator line
        {
            Rect line = GUILayoutUtility.GetRect(windowW, 1f);
            EditorGUI.DrawRect(line, new Color(0.4f, 0.4f, 0.4f));
        }

        for (int i = 0; i < data.waves.Count; i++)
        {
            WaveConfig w      = data.waves[i];
            Rect rowRect      = GUILayoutUtility.GetRect(windowW, RowHeight + BarPad * 2f);
            bool isSelected   = timelineSelectedWave == i;

            // Row background
            if (isSelected)
                EditorGUI.DrawRect(rowRect, new Color(0.22f, 0.36f, 0.52f, 0.35f));
            else if (i % 2 == 0)
                EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.07f));

            // Wave label
            Rect labelR = new Rect(rowRect.x + 2f, rowRect.y + BarPad, LabelWidth - 4f, RowHeight);
            GUIStyle labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.miniLabel;
            GUI.Label(labelR, $"W{i + 1}", labelStyle);

            // Bar area origin
            float bx = rowRect.x + LabelWidth;
            float by = rowRect.y + BarPad;
            float bh = RowHeight;

            // Spawn bar
            float spawnX = bx + (startTimes[i] / totalSpan) * availBarW;
            float spawnW = Mathf.Max(MinBarW, (spawnDurations[i] / totalSpan) * availBarW);

            Color poolCol = GetPoolColor(w.enemyPoolName);
            EditorGUI.DrawRect(new Rect(spawnX, by, spawnW, bh), poolCol);

            // Label inside spawn bar
            if (spawnW > 50f)
            {
                string barLabel = $"{w.enemyPoolName}  ×{w.enemyCount}";
                GUI.Label(new Rect(spawnX + 3f, by + 1f, spawnW - 6f, bh - 2f),
                    barLabel, EditorStyles.whiteMiniLabel);
            }

            // Cooldown bar (thinner, centred vertically)
            float coolX = spawnX + spawnW;
            float coolW = (w.cooldownAfter / totalSpan) * availBarW;
            if (coolW > 1f)
            {
                float ch = bh * 0.35f;
                EditorGUI.DrawRect(
                    new Rect(coolX, by + (bh - ch) * 0.5f, coolW, ch),
                    new Color(0.38f, 0.38f, 0.38f, 0.75f));
            }

            // Row click → select wave
            if (Event.current.type == EventType.MouseDown &&
                rowRect.Contains(Event.current.mousePosition))
            {
                timelineSelectedWave = (timelineSelectedWave == i) ? -1 : i;

                // Also unfold the wave in list view so switching modes shows it open
                if (timelineSelectedWave == i && waveFoldouts.Length > selectedSpawner)
                {
                    bool[] fo = waveFoldouts[selectedSpawner];
                    if (fo != null && i < fo.Length) fo[i] = true;
                }

                Event.current.Use();
                Repaint();
            }
        }

        // Bottom separator
        {
            Rect line = GUILayoutUtility.GetRect(windowW, 1f);
            EditorGUI.DrawRect(line, new Color(0.4f, 0.4f, 0.4f));
        }

        EditorGUILayout.EndScrollView();

        // ── Selected wave detail ───────────────────────────────────────────
        if (timelineSelectedWave >= 0 && timelineSelectedWave < data.waves.Count)
        {
            int     sel  = timelineSelectedWave;
            WaveConfig w = data.waves[sel];

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");

            // Header row with color swatch
            EditorGUILayout.BeginHorizontal();
            Color swatch = GetPoolColor(w.enemyPoolName);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(10f, 16f, GUILayout.Width(10f)), swatch);
            EditorGUILayout.LabelField($"Wave {sel + 1}  —  {w.waveName}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            string newName  = EditorGUILayout.TextField  ("Name",               w.waveName);
            string newPool  = EditorGUILayout.TextField  ("Enemy Pool",         w.enemyPoolName);
            int    newCount = EditorGUILayout.IntField   ("Enemy Count",        w.enemyCount);
            float  newInt   = EditorGUILayout.FloatField ("Spawn Interval (s)", w.spawnInterval);
            float  newCool  = EditorGUILayout.FloatField ("Cooldown After (s)", w.cooldownAfter);

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentMap, "Edit Wave (Timeline)");
                w.waveName      = newName;
                w.enemyPoolName = newPool;
                w.enemyCount    = Mathf.Max(1, newCount);
                w.spawnInterval = Mathf.Max(0f, newInt);
                w.cooldownAfter = Mathf.Max(0f, newCool);
                EditorUtility.SetDirty(currentMap);
            }

            float dur = spawnDurations[sel];
            EditorGUILayout.LabelField(
                $"Start: {startTimes[sel]:0.0}s   Spawn duration: {dur:0.0}s   " +
                $"Cooldown: {w.cooldownAfter:0.0}s   Total slot: {dur + w.cooldownAfter:0.0}s",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Draws a time axis with evenly spaced tick marks and second labels.
    /// </summary>
    private static void DrawTimeAxis(Rect rect, float labelWidth, float availW,
                                     float totalSpan, int tickCount)
    {
        float xOrigin = rect.x + labelWidth;

        // Baseline
        EditorGUI.DrawRect(new Rect(xOrigin, rect.yMax - 1f, availW, 1f),
            new Color(0.55f, 0.55f, 0.55f));

        for (int t = 0; t <= tickCount; t++)
        {
            float frac    = (float)t / tickCount;
            float timeSec = frac * totalSpan;
            float x       = xOrigin + frac * availW;

            // Tick
            EditorGUI.DrawRect(new Rect(x - 0.5f, rect.yMax - 5f, 1f, 5f),
                new Color(0.55f, 0.55f, 0.55f));

            // Label
            string lbl = timeSec >= 60f
                ? $"{timeSec / 60f:0.0}m"
                : $"{timeSec:0}s";

            GUI.Label(new Rect(x - 14f, rect.y, 30f, rect.height - 6f),
                lbl, EditorStyles.centeredGreyMiniLabel);
        }
    }

    /// <summary>
    /// Returns a deterministic, visually distinct colour for a pool name.
    /// The same string always yields the same hue.
    /// </summary>
    private static Color GetPoolColor(string poolName)
    {
        if (string.IsNullOrEmpty(poolName)) return new Color(0.5f, 0.5f, 0.5f);

        int hash = 17;
        foreach (char c in poolName)
            hash = hash * 31 + c;

        float hue = (Mathf.Abs(hash) % 1000) / 1000f;
        return Color.HSVToRGB(hue, 0.62f, 0.80f);
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
