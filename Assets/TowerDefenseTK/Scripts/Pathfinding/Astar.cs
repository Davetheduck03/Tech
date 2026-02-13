using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefenseTK
{
    public class Astar : MonoBehaviour
    {
        public static Astar Instance;

        [Header("References")]
        [SerializeField] private EnemySpawner spawner;

        [Header("Debug")]
        [SerializeField] private bool logPathfinding = true;

        public List<PathNode> allNodes = new List<PathNode>();
        public Dictionary<(PathNode, PathNode), List<PathNode>> generatedPathCache = new Dictionary<(PathNode, PathNode), List<PathNode>>();

        private HashSet<UnitPathFollower> activeFollowers = new HashSet<UnitPathFollower>();

        public event System.Action OnPathsRecalculated;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            PathNodeGenerator.OnGridGenerated += OnGridGenerated;
        }

        private void OnDisable()
        {
            PathNodeGenerator.OnGridGenerated -= OnGridGenerated;
        }

        private void OnGridGenerated()
        {
            StartCoroutine(DelayedPathComputation());
        }

        private IEnumerator DelayedPathComputation()
        {
            yield return new WaitForEndOfFrame();
            PrecomputeAllPaths();
        }

        /// <summary>
        /// Register a path follower to receive path updates
        /// </summary>
        public void RegisterFollower(UnitPathFollower follower)
        {
            activeFollowers.Add(follower);
        }

        /// <summary>
        /// Unregister a path follower
        /// </summary>
        public void UnregisterFollower(UnitPathFollower follower)
        {
            activeFollowers.Remove(follower);
        }

        /// <summary>
        /// Precompute paths between all spawn and exit points
        /// </summary>
        private void PrecomputeAllPaths()
        {
            if (logPathfinding)
                Debug.Log("Astar: Precomputing all paths...");

            generatedPathCache.Clear();

            if (!NodeGetter.nodeValue.ContainsKey(NodeType.Start) ||
                !NodeGetter.nodeValue.ContainsKey(NodeType.End))
            {
                Debug.LogWarning("Astar: No spawn or exit points registered!");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
            {
                foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
                {
                    if (startNode != null && endNode != null)
                    {
                        var path = FindPath(startNode, endNode);
                        if (path != null && path.Count > 0)
                        {
                            generatedPathCache[(startNode, endNode)] = path;
                            successCount++;

                            if (logPathfinding)
                                Debug.Log($"Astar: ✓ Path {startNode.name} → {endNode.name}: {path.Count} nodes");
                        }
                        else
                        {
                            failCount++;
                            Debug.LogWarning($"Astar: ✗ No path from {startNode.name} to {endNode.name}");
                        }
                    }
                }
            }

            Debug.Log($"Astar: Precomputation complete. Success: {successCount}, Failed: {failCount}");

            // Initialize spawner if present
            if (spawner != null)
            {
                spawner.Init();
            }
        }

        /// <summary>
        /// Get a path from cache or compute new one
        /// </summary>
        public List<PathNode> GetPath(PathNode start, PathNode goal)
        {
            if (start == null || goal == null) return null;

            var key = (start, goal);

            // Check cache first
            if (generatedPathCache.TryGetValue(key, out List<PathNode> cachedPath))
            {
                if (IsPathValid(cachedPath))
                {
                    return new List<PathNode>(cachedPath); // Return a copy
                }

                // Cache invalid, recalculate
                if (logPathfinding)
                    Debug.Log($"Astar: Cached path invalid, recalculating {start.name} → {goal.name}");

                List<PathNode> newPath = FindPath(start, goal);
                if (newPath != null)
                {
                    generatedPathCache[key] = newPath;
                }
                return newPath;
            }

            // Not in cache, calculate
            return FindPath(start, goal);
        }

        /// <summary>
        /// Check if placing a tower at coords would block all paths
        /// </summary>
        public bool WouldBlockAllPaths(Vector2Int coords)
        {
            PathNode nodeToBlock = PathNodeGenerator.Instance?.GetNodeAt(coords);
            if (nodeToBlock == null) return false;

            // Temporarily mark as unwalkable
            bool originalWalkable = nodeToBlock.isWalkable;
            nodeToBlock.isWalkable = false;

            bool anyPathExists = false;

            // Check if any path still exists
            foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
            {
                foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
                {
                    if (startNode != null && endNode != null)
                    {
                        var testPath = FindPath(startNode, endNode);
                        if (testPath != null && testPath.Count > 0)
                        {
                            anyPathExists = true;
                            break;
                        }
                    }
                }
                if (anyPathExists) break;
            }

            // Restore original state
            nodeToBlock.isWalkable = originalWalkable;

            return !anyPathExists;
        }

        /// <summary>
        /// Check if a path is valid (all nodes walkable)
        /// </summary>
        private bool IsPathValid(List<PathNode> path)
        {
            if (path == null || path.Count == 0) return false;

            foreach (var node in path)
            {
                if (node == null || !node.isWalkable)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// A* pathfinding algorithm
        /// </summary>
        public List<PathNode> FindPath(PathNode start, PathNode goal)
        {
            if (start == null || goal == null) return null;
            if (!start.isWalkable || !goal.isWalkable) return null;

            var openSet = new List<PathNode> { start };
            var closedSet = new HashSet<PathNode>();

            // Reset costs
            foreach (var node in allNodes)
            {
                node.gCost = Mathf.Infinity;
                node.hCost = 0;
                node.parent = null;
            }

            start.gCost = 0;
            start.hCost = Heuristic(start, goal);

            while (openSet.Count > 0)
            {
                // Get node with lowest fCost
                PathNode current = openSet.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();

                if (current == goal)
                {
                    return ReconstructPath(start, goal);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in current.neighbors)
                {
                    if (closedSet.Contains(neighbor) || !neighbor.isWalkable)
                        continue;

                    float tentativeG = current.gCost + GetMovementCost(current, neighbor);

                    if (tentativeG < neighbor.gCost)
                    {
                        neighbor.parent = current;
                        neighbor.gCost = tentativeG;
                        neighbor.hCost = Heuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return null; // No path found
        }

        private float Heuristic(PathNode a, PathNode b)
        {
            // Manhattan distance for 4-directional movement
            float dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
            float dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);

            // Add small tie-breaker to prefer straighter paths
            // This breaks ties by slightly preferring paths closer to a straight line
            float cross = Mathf.Abs(dx - dy) * 0.001f;

            return dx + dy + cross;
        }

        private float GetMovementCost(PathNode from, PathNode to)
        {
            // Base cost is 1 for adjacent cells
            // Could add terrain costs here based on TileType
            return 1f;
        }

        private List<PathNode> ReconstructPath(PathNode start, PathNode goal)
        {
            List<PathNode> path = new List<PathNode>();
            PathNode current = goal;

            while (current != null)
            {
                path.Add(current);
                if (current == start) break;
                current = current.parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Clear path cache
        /// </summary>
        public void ClearCache()
        {
            generatedPathCache.Clear();
        }

        /// <summary>
        /// Recalculate all paths and update active followers
        /// </summary>
        public void RecalculateAndCacheAllPaths()
        {
            if (logPathfinding)
                Debug.Log("=== RECALCULATING ALL PATHS ===");

            generatedPathCache.Clear();

            int successCount = 0;
            int failCount = 0;

            foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
            {
                foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
                {
                    if (startNode != null && endNode != null)
                    {
                        var path = FindPath(startNode, endNode);
                        if (path != null && path.Count > 0)
                        {
                            generatedPathCache[(startNode, endNode)] = path;
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                }
            }

            if (logPathfinding)
                Debug.Log($"=== RECALCULATION COMPLETE: {successCount} success, {failCount} failed ===");

            // Notify listeners
            OnPathsRecalculated?.Invoke();

            // Update all active followers
            ReassignAllActivePaths();
        }

        private void ReassignAllActivePaths()
        {
            if (logPathfinding)
                Debug.Log($"Astar: Reassigning paths to {activeFollowers.Count} active units");

            List<UnitPathFollower> followers = new List<UnitPathFollower>(activeFollowers);

            foreach (var follower in followers)
            {
                if (follower != null)
                {
                    follower.RequestPathUpdate();
                }
            }
        }
    }
}