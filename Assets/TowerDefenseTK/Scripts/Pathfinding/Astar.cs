using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefenseTK
{
    public class Astar : MonoBehaviour
    {
        public static Astar Instance;
        private void Awake() => Instance = this;

        public List<PathNode> allNodes = new List<PathNode>();
        public Dictionary<(PathNode, PathNode), List<PathNode>> generatedPathCache = new Dictionary<(PathNode, PathNode), List<PathNode>>();

        // Track all active path followers for recalculation
        private HashSet<UnitPathFollower> activeFollowers = new HashSet<UnitPathFollower>();

        // Event for when paths are recalculated
        public event System.Action OnPathsRecalculated;



        [SerializeField] private EnemySpawner spawner; // Placeholder for Showcase purposes




        private void OnEnable()
        {
            PathNodeGenerator.OnGridGenerated += DelayComputePath;
        }

        private void OnDisable()
        {
            PathNodeGenerator.OnGridGenerated -= DelayComputePath;
        }

        /// <summary>
        /// Register a path follower to be managed by Astar
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

        private void PrecomputeAllPaths()
        {
            Debug.Log("Precomputing all paths...");

            foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
            {
                foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
                {
                    if (startNode != null && endNode != null)
                    {
                        var path = FindPath(startNode, endNode);
                        if (path != null)
                        {
                            generatedPathCache[(startNode, endNode)] = path;
                            Debug.Log($"Precomputed path from {startNode.name} to {endNode.name}: {path.Count} nodes");
                        }
                    }
                }
            }

            Debug.Log($"Precomputation complete. Total paths: {generatedPathCache.Count}");
            spawner.Init();
        }

        private void DelayComputePath()
        {
            StartCoroutine(C_DelayComputePath());
        }

        private IEnumerator C_DelayComputePath()
        {
            yield return new WaitForEndOfFrame();
            PrecomputeAllPaths();
        }

        /// <summary>
        /// Get a cached path or compute a new one
        /// </summary>
        public List<PathNode> GetPath(PathNode start, PathNode goal)
        {
            if (start == null || goal == null) return null;

            var key = (start, goal);

            // Check cache first
            if (generatedPathCache.ContainsKey(key))
            {
                List<PathNode> cachedPath = generatedPathCache[key];

                // Validate the cached path - check if all nodes are walkable
                if (IsPathValid(cachedPath))
                {
                    return cachedPath;
                }
                else
                {
                    // Path is invalid, recalculate
                    Debug.Log($"Cached path invalid, recalculating from {start.name} to {goal.name}");
                    List<PathNode> newPath = FindPath(start, goal);
                    if (newPath != null)
                    {
                        generatedPathCache[key] = newPath;
                    }
                    return newPath;
                }
            }

            // Not in cache, calculate new path
            return FindPath(start, goal);
        }

        /// <summary>
        /// Check if a path is valid (all nodes are walkable)
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

        public List<PathNode> FindPath(PathNode start, PathNode goal)
        {
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<PathNode>();
            openSet.Add(start);

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
                var current = openSet.OrderBy(n => n.fCost).First();

                if (current == goal)
                {
                    var path = ReconstructPath(start, goal);
                    return path;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in current.neighbors)
                {
                    if (closedSet.Contains(neighbor) || !neighbor.isWalkable)
                        continue;

                    float tentativeG = current.gCost + Vector3.Distance(current.transform.position, neighbor.transform.position);

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

            Debug.LogWarning($"No path found from {start.name} to {goal.name}");
            return null;
        }

        private float Heuristic(PathNode a, PathNode b)
        {
            return Vector3.Distance(a.transform.position, b.transform.position);
        }

        private List<PathNode> ReconstructPath(PathNode start, PathNode goal)
        {
            List<PathNode> path = new List<PathNode>();
            PathNode current = goal;

            while (current != null)
            {
                path.Add(current);
                current = current.parent;
            }

            path.Reverse();
            return path;
        }

        public void ClearCache()
        {
            generatedPathCache.Clear();
        }

        /// <summary>
        /// Recalculates all paths and forces all active units to update.
        /// Call this when obstacles are placed or removed.
        /// </summary>
        public void RecalculateAndCacheAllPaths()
        {
            Debug.Log("=== RECALCULATING ALL PATHS ===");

            // Clear existing cached paths
            generatedPathCache.Clear();

            // Recalculate all start->end combinations
            int successCount = 0;
            int failCount = 0;

            foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
            {
                foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
                {
                    if (startNode != null && endNode != null)
                    {
                        var path = FindPath(startNode, endNode);
                        if (path != null)
                        {
                            generatedPathCache[(startNode, endNode)] = path;
                            successCount++;
                            Debug.Log($"✓ Cached new path: {startNode.name} → {endNode.name} ({path.Count} nodes)");
                        }
                        else
                        {
                            failCount++;
                            Debug.LogWarning($"✗ No path found: {startNode.name} → {endNode.name}");
                        }
                    }
                }
            }

            Debug.Log($"=== PATH RECALCULATION COMPLETE ===");
            Debug.Log($"Success: {successCount} | Failed: {failCount}");

            // Notify all listeners that paths have been recalculated
            OnPathsRecalculated?.Invoke();

            // Force all active units to update their paths
            ReassignAllActivePaths();
        }

        /// <summary>
        /// Forces all active path followers to fetch new paths from cache
        /// </summary>
        private void ReassignAllActivePaths()
        {
            Debug.Log($"Reassigning paths to {activeFollowers.Count} active units");

            // Create a copy to avoid modification during iteration
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