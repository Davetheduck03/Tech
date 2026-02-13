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
        [Tooltip("If true, automatically adds EnemySpawner to Spawn nodes")]
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
            if (!showDebugGizmos || !Application.isPlaying) return;

            foreach (var kvp in pathNodes)
            {
                PathNode node = kvp.Value;
                Vector3 pos = node.transform.position;

                // Color based on tile type
                Gizmos.color = node.TileType switch
                {
                    TileType.Path => new Color(0.6f, 0.5f, 0.3f, 0.5f),
                    TileType.Blocked => new Color(0.2f, 0.2f, 0.2f, 0.5f),
                    TileType.Buildable => new Color(0.3f, 0.6f, 0.3f, 0.5f),
                    TileType.Spawn => new Color(0.2f, 0.5f, 0.8f, 0.8f),
                    TileType.Exit => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                    TileType.Hybrid => node.HasTower
                        ? new Color(0.8f, 0.4f, 0.1f, 0.7f)
                        : new Color(0.9f, 0.7f, 0.2f, 0.6f),
                    _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)
                };

                Gizmos.DrawCube(pos, Vector3.one * 0.4f);

                // Draw X for non-walkable
                if (!node.isWalkable)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos + new Vector3(-0.2f, 0, -0.2f), pos + new Vector3(0.2f, 0, 0.2f));
                    Gizmos.DrawLine(pos + new Vector3(-0.2f, 0, 0.2f), pos + new Vector3(0.2f, 0, -0.2f));
                }
            }
        }
#endif

        #endregion
    }
}