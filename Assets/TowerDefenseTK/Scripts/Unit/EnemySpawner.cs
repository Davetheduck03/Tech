using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Spawns enemies at a start node and directs them to an assigned or auto-detected end node.
    /// If no end node is assigned, finds the closest reachable end node via Astar.
    /// Attach this to your Start/Spawn PathNode GameObject.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private string enemyPoolName = "Basic Enemy";
        [SerializeField] private int enemiesToSpawn = 5;
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private float waveCooldown = 10f;
        [SerializeField] private bool spawnOnStart = true;

        [Header("Target Settings")]
        [Tooltip("Assign a specific end node. If empty, finds the closest reachable end node via Astar.")]
        [SerializeField] private Transform assignedEndNode;

        [Header("Initialization")]
        [Tooltip("If true, auto-initializes when Astar finishes computing paths. If false, call Init() manually.")]
        [SerializeField] private bool autoInitialize = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Runtime state
        private Transform targetEndNode;
        private PathNode startPathNode;
        private PathNode endPathNode;
        private int currentWave = 1;
        private bool isSpawning = false;
        private bool isInitialized = false;

        // Public accessors
        public int CurrentWave => currentWave;
        public bool IsSpawning => isSpawning;
        public Transform TargetEndNode => targetEndNode;
        public bool IsInitialized => isInitialized;

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Subscribe to grid generation event
            PathNodeGenerator.OnGridGenerated += OnGridGenerated;
        }

        private void OnDisable()
        {
            PathNodeGenerator.OnGridGenerated -= OnGridGenerated;
        }

        private void Start()
        {
            // If grid is already generated (spawner added late), try to initialize
            if (autoInitialize && PathNodeGenerator.Instance != null && !isInitialized)
            {
                StartCoroutine(DelayedInit());
            }
        }

        private void OnGridGenerated()
        {
            if (autoInitialize && !isInitialized)
            {
                StartCoroutine(DelayedInit());
            }
        }

        /// <summary>
        /// Delay initialization to ensure Astar has computed paths
        /// </summary>
        private IEnumerator DelayedInit()
        {
            // Wait for Astar to finish computing paths
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Extra wait to ensure paths are cached
            yield return new WaitForSeconds(0.1f);

            Init();
        }

        #endregion

        #region Initialization

        public void Init()
        {
            if (isInitialized) return;

            if (showDebugLogs)
                Debug.Log($"EnemySpawner '{name}': Initializing...");

            // Get our start PathNode
            startPathNode = GetComponent<PathNode>()
                         ?? GetComponentInParent<PathNode>()
                         ?? GetPathNodeAtPosition(transform.position);

            if (startPathNode == null)
            {
                Debug.LogError($"EnemySpawner '{name}': No PathNode found at spawn position! " +
                              "Make sure this spawner is on or under a PathNode, or the PathNodeGenerator has run.");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"EnemySpawner '{name}': Found start node '{startPathNode.name}'");

            // Setup end node (assigned or find closest)
            if (!SetupEndNode())
            {
                Debug.LogError($"EnemySpawner '{name}': Could not find valid end node! " +
                              "Make sure there's at least one Exit node and a valid path exists.");
                return;
            }

            isInitialized = true;

            if (showDebugLogs)
                Debug.Log($"EnemySpawner '{name}': ✓ Initialized → Target: {targetEndNode.name}");

            if (spawnOnStart)
            {
                StartCoroutine(SpawnWaves());
            }
        }

        private bool SetupEndNode()
        {
            // Wait for Astar if not ready
            if (Astar.Instance == null)
            {
                Debug.LogWarning("EnemySpawner: Astar.Instance is null! Cannot setup end node yet.");
                return false;
            }

            // Option 1: Use assigned end node if valid
            if (assignedEndNode != null)
            {
                endPathNode = assignedEndNode.GetComponent<PathNode>()
                           ?? GetPathNodeAtPosition(assignedEndNode.position);

                if (endPathNode != null && IsPathValid(startPathNode, endPathNode))
                {
                    targetEndNode = assignedEndNode;

                    if (showDebugLogs)
                        Debug.Log($"EnemySpawner: Using assigned end node '{assignedEndNode.name}'");

                    return true;
                }

                Debug.LogWarning($"EnemySpawner: Assigned end node '{assignedEndNode.name}' not reachable, finding closest...");
            }

            // Option 2: Find closest reachable end node via Astar
            return FindClosestEndNode();
        }

        private bool FindClosestEndNode()
        {
            if (Astar.Instance == null)
            {
                Debug.LogError("EnemySpawner: Astar.Instance is null!");
                return false;
            }

            // Get all registered end nodes
            if (!NodeGetter.nodeValue.ContainsKey(NodeType.End) ||
                NodeGetter.nodeValue[NodeType.End].Count == 0)
            {
                Debug.LogError("EnemySpawner: No end nodes registered in NodeGetter! " +
                              "Make sure you have Exit tiles in your MapData.");
                return false;
            }

            if (showDebugLogs)
                Debug.Log($"EnemySpawner: Searching {NodeGetter.nodeValue[NodeType.End].Count} end nodes...");

            PathNode closestNode = null;
            float shortestDistance = float.MaxValue;

            foreach (PathNode endNode in NodeGetter.nodeValue[NodeType.End])
            {
                if (endNode == null) continue;

                // Get path via Astar
                List<PathNode> path = Astar.Instance.GetPath(startPathNode, endNode);

                if (path == null || path.Count == 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"EnemySpawner: No path to '{endNode.name}'");
                    continue;
                }

                // Calculate total path distance
                float distance = CalculatePathDistance(path);

                if (showDebugLogs)
                    Debug.Log($"EnemySpawner: Path to '{endNode.name}' = {distance:F1} units ({path.Count} nodes)");

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestNode = endNode;
                }
            }

            if (closestNode != null)
            {
                endPathNode = closestNode;
                targetEndNode = closestNode.transform;

                if (showDebugLogs)
                    Debug.Log($"EnemySpawner: ✓ Selected closest end node '{closestNode.name}' (distance: {shortestDistance:F1})");

                return true;
            }

            Debug.LogError("EnemySpawner: No reachable end node found!");
            return false;
        }

        private bool IsPathValid(PathNode from, PathNode to)
        {
            if (Astar.Instance == null) return false;

            List<PathNode> path = Astar.Instance.GetPath(from, to);
            return path != null && path.Count > 0;
        }

        private float CalculatePathDistance(List<PathNode> path)
        {
            if (path == null || path.Count < 2) return 0f;

            float total = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                total += Vector3.Distance(path[i].transform.position, path[i + 1].transform.position);
            }
            return total;
        }

        private PathNode GetPathNodeAtPosition(Vector3 position)
        {
            if (PathNodeGenerator.Instance != null)
            {
                return PathNodeGenerator.Instance.GetNodeAtWorldPosition(position);
            }
            return null;
        }

        #endregion

        #region Wave Spawning

        private IEnumerator SpawnWaves()
        {
            isSpawning = true;

            while (true)
            {
                Debug.Log($"=== Wave {currentWave} starting ===");

                yield return StartCoroutine(SpawnWaveEnemies());

                Debug.Log($"=== Wave {currentWave} finished. Next wave in {waveCooldown}s ===");

                yield return new WaitForSeconds(waveCooldown);

                currentWave++;
            }
        }

        private IEnumerator SpawnWaveEnemies()
        {
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy(i);

                if (i < enemiesToSpawn - 1)
                    yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnEnemy(int index)
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogError("EnemySpawner: PoolManager.Instance is null!");
                return;
            }

            GameObject spawnedEnemy = PoolManager.Instance.Spawn(
                enemyPoolName,
                transform.position,
                Quaternion.identity
            );

            if (spawnedEnemy != null)
            {
                if (showDebugLogs)
                    Debug.Log($"Enemy {index + 1}/{enemiesToSpawn} spawned (Wave {currentWave}) → {targetEndNode.name}");

                // Set target and trigger movement
                MovementComponent movementComp = spawnedEnemy.GetComponent<MovementComponent>();
                if (movementComp != null)
                {
                    movementComp.targetTransform = targetEndNode;
                    movementComp.OnTriggerMove();
                }
                else
                {
                    Debug.LogWarning($"EnemySpawner: Spawned enemy has no MovementComponent!");
                }
            }
            else
            {
                Debug.LogError($"EnemySpawner: Failed to spawn enemy from pool '{enemyPoolName}'! " +
                              "Check that the pool exists in PoolManager.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually trigger wave spawning
        /// </summary>
        public void TriggerSpawn()
        {
            if (!isInitialized) Init();

            if (!isSpawning && isInitialized)
            {
                StartCoroutine(SpawnWaves());
            }
        }

        /// <summary>
        /// Stop spawning waves
        /// </summary>
        public void StopSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
        }

        /// <summary>
        /// Change end node at runtime
        /// </summary>
        public void SetEndNode(Transform newEndNode)
        {
            assignedEndNode = newEndNode;
            if (isInitialized) SetupEndNode();
        }

        /// <summary>
        /// Recalculate path (call after map changes)
        /// </summary>
        public void RecalculatePath()
        {
            if (!isInitialized) return;
            SetupEndNode();
        }

        /// <summary>
        /// Force re-initialization
        /// </summary>
        public void Reinitialize()
        {
            isInitialized = false;
            Init();
        }

        /// <summary>
        /// Configure spawner settings (called by PathNodeGenerator when auto-attaching)
        /// </summary>
        public void Configure(string poolName, int enemyCount, float interval, float cooldown, bool autoInit = true)
        {
            enemyPoolName = poolName;
            enemiesToSpawn = enemyCount;
            spawnInterval = interval;
            waveCooldown = cooldown;
            autoInitialize = autoInit;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            // Draw spawn point
            Gizmos.color = isInitialized ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw "S" label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "SPAWN");
#endif

            // Draw line to assigned end node
            if (assignedEndNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                               assignedEndNode.position + Vector3.up * 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // In play mode, show actual target and path
            if (Application.isPlaying && targetEndNode != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                               targetEndNode.position + Vector3.up * 0.5f);
                Gizmos.DrawWireSphere(targetEndNode.position, 0.6f);

                // Draw path if available
                if (Astar.Instance != null && startPathNode != null && endPathNode != null)
                {
                    List<PathNode> path = Astar.Instance.GetPath(startPathNode, endPathNode);
                    if (path != null && path.Count > 1)
                    {
                        Gizmos.color = Color.magenta;
                        for (int i = 0; i < path.Count - 1; i++)
                        {
                            Gizmos.DrawLine(
                                path[i].transform.position + Vector3.up * 0.3f,
                                path[i + 1].transform.position + Vector3.up * 0.3f
                            );
                        }
                    }
                }
            }
        }

        #endregion
    }
}