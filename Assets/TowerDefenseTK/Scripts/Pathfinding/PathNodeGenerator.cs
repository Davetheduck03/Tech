using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class PathNodeGenerator : MonoBehaviour
    {
        public static event Action OnGridGenerated;
        public static PathNodeGenerator Instance;

        [Header("Node Settings")]
        public GameObject nodePrefab;

        [Header("Map Data")]
        [SerializeField] private MapData mapData;

        [Header("Spawner Settings")]
        [Tooltip("If true, automatically adds EnemySpawner to Spawn nodes and ExitZone to Exit nodes")]
        [SerializeField] private bool autoAttachSpawners = true;
        [SerializeField] private string defaultEnemyPoolName = "Basic Enemy";
        [SerializeField] private int defaultEnemiesToSpawn = 5;
        [SerializeField] private float defaultSpawnInterval = 0.5f;
        [SerializeField] private float defaultWaveCooldown = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private Dictionary<Vector2Int, PathNode> pathNodes = new Dictionary<Vector2Int, PathNode>();
        private List<EnemySpawner> spawnedSpawners = new List<EnemySpawner>();

        // Public access
        public Dictionary<Vector2Int, PathNode> PathNodes => pathNodes;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Try to get MapData from MapLoader if not assigned
            if (mapData == null)
            {
                MapLoader loader = FindFirstObjectByType<MapLoader>();
                if (loader != null)
                {
                    mapData = loader.mapData;
                }
            }

            GenerateNodes();
            LinkNeighbors();
            ApplyMapData();
            RegisterSpecialNodes();

            if (autoAttachSpawners)
            {
                AttachSpawnersToSpawnNodes();
                AttachExitZonesToExitNodes();
            }

            StartCoroutine(DelayedGridGenerated());
        }

        private IEnumerator DelayedGridGenerated()
        {
            yield return new WaitForEndOfFrame();
            OnGridGenerated?.Invoke();
        }

        #region Node Generation

        private void GenerateNodes()
        {
            GridManager gm = GridManager.Instance;
            if (gm == null)
            {
                Debug.LogError("PathNodeGenerator: GridManager not found!");
                return;
            }

            pathNodes.Clear();

            foreach (var kvp in gm.GetAllNodes())
            {
                Vector2Int coords = kvp.Key;
                GridNode gridNode = kvp.Value;

                Vector3 spawnPos = gridNode.worldPos + new Vector3(
                    gm.cellSize / 2f,
                    0.75f,
                    gm.cellSize / 2f
                );

                GameObject nodeObj = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);
                nodeObj.name = $"PathNode ({coords.x},{coords.y})";

                PathNode pathNode = nodeObj.GetComponent<PathNode>();
                pathNode.gridPosition = coords;
                pathNode.isWalkable = true;

                pathNodes.Add(coords, pathNode);

                if (Astar.Instance != null)
                {
                    Astar.Instance.allNodes.Add(pathNode);
                }
            }

            Debug.Log($"PathNodeGenerator: Created {pathNodes.Count} nodes");
        }

        private void LinkNeighbors()
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(1, 0),   // Right
                new Vector2Int(0, -1),  // Down
                new Vector2Int(0, 1)    // Up
            };

            foreach (var kvp in pathNodes)
            {
                Vector2Int coord = kvp.Key;
                PathNode node = kvp.Value;
                node.neighbors.Clear();

                foreach (var dir in directions)
                {
                    Vector2Int neighborCoord = coord + dir;
                    if (pathNodes.TryGetValue(neighborCoord, out PathNode neighbor))
                    {
                        node.neighbors.Add(neighbor);
                    }
                }
            }
        }

        #endregion

        #region MapData Application

        private void ApplyMapData()
        {
            if (mapData == null)
            {
                Debug.Log("PathNodeGenerator: No MapData assigned, using default walkability");
                return;
            }

            foreach (var tileData in mapData.tiles)
            {
                if (pathNodes.TryGetValue(tileData.coords, out PathNode node))
                {
                    node.SetTileType(tileData.type);
                }
            }

            Debug.Log($"PathNodeGenerator: Applied {mapData.tiles.Count} tile configurations");
        }

        private void RegisterSpecialNodes()
        {
            // Initialize dictionaries if needed
            if (!NodeGetter.nodeValue.ContainsKey(NodeType.Start))
                NodeGetter.nodeValue[NodeType.Start] = new List<PathNode>();
            else
                NodeGetter.nodeValue[NodeType.Start].Clear();

            if (!NodeGetter.nodeValue.ContainsKey(NodeType.End))
                NodeGetter.nodeValue[NodeType.End] = new List<PathNode>();
            else
                NodeGetter.nodeValue[NodeType.End].Clear();

            foreach (var kvp in pathNodes)
            {
                PathNode node = kvp.Value;

                if (node.IsSpawnPoint)
                {
                    NodeGetter.nodeValue[NodeType.Start].Add(node);
                    Debug.Log($"PathNodeGenerator: Registered spawn point '{node.name}'");
                }
                else if (node.IsExitPoint)
                {
                    NodeGetter.nodeValue[NodeType.End].Add(node);
                    Debug.Log($"PathNodeGenerator: Registered exit point '{node.name}'");
                }
            }

            Debug.Log($"PathNodeGenerator: {NodeGetter.nodeValue[NodeType.Start].Count} spawn points, " +
                     $"{NodeGetter.nodeValue[NodeType.End].Count} exit points");
        }

        #endregion

        #region Auto Spawner Attachment

        private void AttachSpawnersToSpawnNodes()
        {
            // Clear any previously spawned spawners
            foreach (var spawner in spawnedSpawners)
            {
                if (spawner != null)
                {
                    Destroy(spawner);
                }
            }
            spawnedSpawners.Clear();

            // Find all spawn nodes and attach spawners
            if (!NodeGetter.nodeValue.ContainsKey(NodeType.Start))
            {
                Debug.LogWarning("PathNodeGenerator: No spawn nodes to attach spawners to!");
                return;
            }

            foreach (PathNode spawnNode in NodeGetter.nodeValue[NodeType.Start])
            {
                if (spawnNode == null) continue;

                // Check if already has a spawner
                EnemySpawner existingSpawner = spawnNode.GetComponent<EnemySpawner>();
                if (existingSpawner != null)
                {
                    Debug.Log($"PathNodeGenerator: Spawn node '{spawnNode.name}' already has EnemySpawner");
                    spawnedSpawners.Add(existingSpawner);
                    continue;
                }

                // Add new spawner
                EnemySpawner newSpawner = spawnNode.gameObject.AddComponent<EnemySpawner>();
                ConfigureSpawner(newSpawner);
                spawnedSpawners.Add(newSpawner);

                Debug.Log($"PathNodeGenerator: ✓ Attached EnemySpawner to '{spawnNode.name}'");
            }

            Debug.Log($"PathNodeGenerator: {spawnedSpawners.Count} spawners ready");
        }

        private void ConfigureSpawner(EnemySpawner spawner)
        {
            // Use the public Configure method
            spawner.Configure(
                defaultEnemyPoolName,
                defaultEnemiesToSpawn,
                defaultSpawnInterval,
                defaultWaveCooldown,
                autoInit: true
            );
        }

        #endregion

        #region Auto ExitZone Attachment

        private void AttachExitZonesToExitNodes()
        {
            if (!NodeGetter.nodeValue.ContainsKey(NodeType.End))
            {
                Debug.LogWarning("PathNodeGenerator: No exit nodes to attach ExitZones to!");
                return;
            }

            int count = 0;

            foreach (PathNode exitNode in NodeGetter.nodeValue[NodeType.End])
            {
                if (exitNode == null) continue;

                // Skip if already has ExitZone
                if (exitNode.GetComponent<ExitZone>() != null)
                {
                    count++;
                    continue;
                }

                // Ensure there's a trigger collider for detection
                Collider col = exitNode.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider box = exitNode.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(
                        GridManager.Instance.cellSize * 0.8f,
                        2f,
                        GridManager.Instance.cellSize * 0.8f
                    );
                }
                else if (!col.isTrigger)
                {
                    // Existing collider is not a trigger, add a separate trigger collider
                    BoxCollider triggerBox = exitNode.gameObject.AddComponent<BoxCollider>();
                    triggerBox.isTrigger = true;
                    triggerBox.size = new Vector3(
                        GridManager.Instance.cellSize * 0.8f,
                        2f,
                        GridManager.Instance.cellSize * 0.8f
                    );
                }

                // Add ExitZone component
                exitNode.gameObject.AddComponent<ExitZone>();
                count++;

                Debug.Log($"PathNodeGenerator: ✓ Attached ExitZone to '{exitNode.name}'");
            }

            Debug.Log($"PathNodeGenerator: {count} exit zones ready");
        }

        #endregion

        #region Public Methods

        public PathNode GetNodeAt(Vector2Int coords)
        {
            pathNodes.TryGetValue(coords, out PathNode node);
            return node;
        }

        public PathNode GetNodeAtWorldPosition(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return null;

            Vector2Int coords = GridManager.Instance.WorldToGrid(worldPos);
            return GetNodeAt(coords);
        }

        public void PlaceTowerOnNode(Vector2Int coords)
        {
            if (pathNodes.TryGetValue(coords, out PathNode node))
            {
                node.PlaceTower();
            }
        }

        public void RemoveTowerFromNode(Vector2Int coords)
        {
            if (pathNodes.TryGetValue(coords, out PathNode node))
            {
                node.RemoveTower();
            }
        }

        // Legacy support
        public void BlockNodeForTower(Vector2Int coords) => PlaceTowerOnNode(coords);
        public void UnblockNode(Vector2Int coords) => RemoveTowerFromNode(coords);

        public List<EnemySpawner> GetSpawners() => spawnedSpawners;

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            if (Application.isPlaying && pathNodes.Count > 0)
            {
                // ── Play mode: draw live PathNode data ────────────────────────
                DrawLiveNodeGizmos();
            }
            else if (!Application.isPlaying && mapData != null)
            {
                // ── Edit mode: draw from MapData so the grid is always visible ─
                DrawMapDataGizmos();
            }
        }

        /// <summary>Draw gizmos using the live runtime PathNode dictionary.</summary>
        private void DrawLiveNodeGizmos()
        {
            foreach (var kvp in pathNodes)
            {
                PathNode node = kvp.Value;
                if (node == null) continue;
                Vector3 pos = node.transform.position;

                Gizmos.color = GetNodeGizmoColor(node.TileType, node.HasTower);
                Gizmos.DrawCube(pos, Vector3.one * 0.4f);

                if (!node.isWalkable)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos + new Vector3(-0.2f, 0, -0.2f), pos + new Vector3(0.2f, 0,  0.2f));
                    Gizmos.DrawLine(pos + new Vector3(-0.2f, 0,  0.2f), pos + new Vector3(0.2f, 0, -0.2f));
                }
            }
        }

        /// <summary>
        /// Draw gizmos in Edit mode by sampling the assigned MapData directly.
        /// GridManager is used to convert grid coords to world positions;
        /// if it is absent we fall back to this transform as the origin.
        /// </summary>
        private void DrawMapDataGizmos()
        {
            // Resolve world-space origin + cell size
            GridManager gm = GridManager.Instance != null
                ? GridManager.Instance
                : FindFirstObjectByType<GridManager>();

            float  cellSz  = gm != null ? gm.cellSize  : mapData.cellSize;
            Vector3 origin = gm != null ? gm.transform.position : transform.position;

            float half = cellSz * 0.5f;

            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    Vector2Int coords = new Vector2Int(x, y);
                    TileData   tile   = mapData.GetTile(coords);
                    TileType   type   = tile != null ? tile.type : TileType.Empty;

                    // Skip empty tiles to keep the view clean
                    if (type == TileType.Empty) continue;

                    // Centre of the cell, slightly above ground
                    Vector3 pos = origin
                        + new Vector3(x * cellSz + half, 0.05f, y * cellSz + half);

                    Gizmos.color = GetNodeGizmoColor(type, hasTower: false);
                    Gizmos.DrawCube(pos, new Vector3(cellSz * 0.7f, 0.05f, cellSz * 0.7f));

                    // Non-walkable cross overlay
                    bool walkable = type == TileType.Empty
                        || type == TileType.Path
                        || type == TileType.Spawn
                        || type == TileType.Exit
                        || type == TileType.Hybrid;

                    if (!walkable)
                    {
                        float r = cellSz * 0.25f;
                        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.85f);
                        Gizmos.DrawLine(pos + new Vector3(-r, 0.01f, -r), pos + new Vector3(r, 0.01f,  r));
                        Gizmos.DrawLine(pos + new Vector3(-r, 0.01f,  r), pos + new Vector3(r, 0.01f, -r));
                    }
                }
            }
        }

        /// <summary>Return a gizmo colour for a given TileType.</summary>
        private static Color GetNodeGizmoColor(TileType type, bool hasTower) => type switch
        {
            TileType.Path      => new Color(0.76f, 0.60f, 0.30f, 0.55f),
            TileType.Blocked   => new Color(0.20f, 0.20f, 0.20f, 0.55f),
            TileType.Buildable => new Color(0.30f, 0.65f, 0.30f, 0.55f),
            TileType.Spawn     => new Color(0.20f, 0.50f, 0.85f, 0.80f),
            TileType.Exit      => new Color(0.85f, 0.20f, 0.20f, 0.80f),
            TileType.Hybrid    => hasTower
                                  ? new Color(0.80f, 0.40f, 0.10f, 0.70f)
                                  : new Color(0.95f, 0.75f, 0.20f, 0.65f),
            _                  => new Color(0.50f, 0.50f, 0.50f, 0.30f)
        };
#endif

        #endregion
    }
}