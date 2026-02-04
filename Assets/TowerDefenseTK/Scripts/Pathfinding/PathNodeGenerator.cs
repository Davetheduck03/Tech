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

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private Dictionary<Vector2Int, PathNode> pathNodes = new Dictionary<Vector2Int, PathNode>();

        // Public access to nodes
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

            StartCoroutine(DelayedGridGenerated());
        }

        private IEnumerator DelayedGridGenerated()
        {
            yield return new WaitForEndOfFrame();
            OnGridGenerated?.Invoke();
        }

        /// <summary>
        /// Generate PathNodes based on GridManager
        /// </summary>
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

        /// <summary>
        /// Link neighboring nodes for pathfinding
        /// </summary>
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

        /// <summary>
        /// Apply MapData tile types to PathNodes
        /// </summary>
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

            Debug.Log($"PathNodeGenerator: Applied {mapData.tiles.Count} tile configurations from MapData");
        }

        /// <summary>
        /// Register spawn and exit nodes with NodeGetter
        /// </summary>
        private void RegisterSpecialNodes()
        {
            // Clear existing registrations
            if (NodeGetter.nodeValue.ContainsKey(NodeType.Start))
                NodeGetter.nodeValue[NodeType.Start].Clear();
            else
                NodeGetter.nodeValue[NodeType.Start] = new List<PathNode>();

            if (NodeGetter.nodeValue.ContainsKey(NodeType.End))
                NodeGetter.nodeValue[NodeType.End].Clear();
            else
                NodeGetter.nodeValue[NodeType.End] = new List<PathNode>();

            foreach (var kvp in pathNodes)
            {
                PathNode node = kvp.Value;

                if (node.IsSpawnPoint)
                {
                    NodeGetter.nodeValue[NodeType.Start].Add(node);
                    Debug.Log($"Registered spawn point: {node.name}");
                }
                else if (node.IsExitPoint)
                {
                    NodeGetter.nodeValue[NodeType.End].Add(node);
                    Debug.Log($"Registered exit point: {node.name}");
                }
            }

            Debug.Log($"PathNodeGenerator: Registered {NodeGetter.nodeValue[NodeType.Start].Count} spawn points, {NodeGetter.nodeValue[NodeType.End].Count} exit points");
        }

        /// <summary>
        /// Get PathNode at grid coordinates
        /// </summary>
        public PathNode GetNodeAt(Vector2Int coords)
        {
            pathNodes.TryGetValue(coords, out PathNode node);
            return node;
        }

        /// <summary>
        /// Get PathNode at world position
        /// </summary>
        public PathNode GetNodeAtWorldPosition(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return null;

            Vector2Int coords = GridManager.Instance.WorldToGrid(worldPos);
            return GetNodeAt(coords);
        }

        /// <summary>
        /// Set tile type at coordinates (runtime modification)
        /// </summary>
        public void SetTileType(Vector2Int coords, TileType type)
        {
            if (pathNodes.TryGetValue(coords, out PathNode node))
            {
                node.SetTileType(type);

                // Update GridManager occupancy
                if (GridManager.Instance != null)
                {
                    bool isOccupied = type == TileType.Blocked || type == TileType.Buildable;
                    GridManager.Instance.SetCellOccupied(coords, isOccupied);
                }
            }
        }

        /// <summary>
        /// Block a node when a tower is placed
        /// </summary>
        public void BlockNodeForTower(Vector2Int coords)
        {
            if (pathNodes.TryGetValue(coords, out PathNode node))
            {
                node.isWalkable = false;
                Debug.Log($"PathNodeGenerator: Blocked node at {coords} for tower");
            }
        }

        /// <summary>
        /// Unblock a node when a tower is removed
        /// </summary>
        public void UnblockNode(Vector2Int coords)
        {
            if (pathNodes.TryGetValue(coords, out PathNode node))
            {
                // Restore walkability based on original tile type
                TileType originalType = TileType.Empty;

                if (mapData != null)
                {
                    TileData tileData = mapData.GetTile(coords);
                    if (tileData != null)
                    {
                        originalType = tileData.type;
                    }
                }

                node.SetTileType(originalType);
                Debug.Log($"PathNodeGenerator: Unblocked node at {coords}");
            }
        }

        /// <summary>
        /// Reload map data at runtime
        /// </summary>
        public void ReloadMapData(MapData newMapData)
        {
            mapData = newMapData;
            ApplyMapData();
            RegisterSpecialNodes();

            // Trigger path recalculation
            if (Astar.Instance != null)
            {
                Astar.Instance.RecalculateAndCacheAllPaths();
            }
        }

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
                    _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)
                };

                Gizmos.DrawCube(pos, Vector3.one * 0.5f);

                // Draw X for non-walkable
                if (!node.isWalkable)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos + new Vector3(-0.3f, 0, -0.3f), pos + new Vector3(0.3f, 0, 0.3f));
                    Gizmos.DrawLine(pos + new Vector3(-0.3f, 0, 0.3f), pos + new Vector3(0.3f, 0, -0.3f));
                }
            }
        }
#endif
    }
}