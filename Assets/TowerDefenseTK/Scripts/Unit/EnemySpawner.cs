using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Per-wave configuration. Each entry in EnemySpawner.waves defines one wave.
    /// When all waves are exhausted, the last wave repeats indefinitely.
    /// </summary>
    [System.Serializable]
    public class WaveConfig
    {
        public string waveName = "Wave";
        public string enemyPoolName = "Basic Enemy";
        [Min(1)] public int enemyCount = 5;
        [Min(0f)] public float spawnInterval = 0.5f;
        [Min(0f)] public float cooldownAfter = 10f;
    }

    /// <summary>
    /// Spawns enemies at a start node and directs them to an assigned or auto-detected end node.
    /// Configure per-wave behaviour via the Waves list in the Inspector.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Waves")]
        [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>()
        {
            new WaveConfig()
        };

        [Header("Target Settings")]
        [Tooltip("Assign a specific end node. If empty, finds the closest reachable end node via Astar.")]
        [SerializeField] private Transform assignedEndNode;

        [Header("Initialization")]
        [SerializeField] private bool spawnOnStart = true;
        [Tooltip("If true, auto-initializes when Astar finishes computing paths. If false, call Init() manually.")]
        [SerializeField] private bool autoInitialize = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Fired when a new wave begins spawning. Arg: 1-based wave number.</summary>
        public static event System.Action<int> OnWaveStarted;

        /// <summary>
        /// Fired when all enemies in a wave have been spawned and the cooldown begins.
        /// Args: wave number that just finished, cooldown duration in seconds.
        /// </summary>
        public static event System.Action<int, float> OnWaveCooldownStarted;

        /// <summary>
        /// Fired immediately after the last enemy of a wave has been spawned.
        /// Arg: 1-based wave number that just completed spawning.
        /// Note: enemies from this wave may still be alive in the scene.
        /// </summary>
        public static event System.Action<int> OnWaveCompleted;

        /// <summary>
        /// Fired once when all explicitly defined waves have been run for the first time.
        /// Arg: total number of defined waves. After this, the spawner loops the last wave.
        /// </summary>
        public static event System.Action<int> OnAllWavesCleared;

        // Runtime state
        private Transform targetEndNode;
        private PathNode startPathNode;
        private PathNode endPathNode;
        private int currentWave = 1;
        private bool isSpawning = false;
        private bool isInitialized = false;
        private bool allWavesCleared = false; // tracks whether OnAllWavesCleared has fired

        // Public accessors
        public int CurrentWave => currentWave;
        public bool IsSpawning => isSpawning;
        public Transform TargetEndNode => targetEndNode;
        public bool IsInitialized => isInitialized;
        public List<WaveConfig> Waves => waves;

        #region Unity Lifecycle

        private void OnEnable()
        {
            PathNodeGenerator.OnGridGenerated += OnGridGenerated;
        }

        private void OnDisable()
        {
            PathNodeGenerator.OnGridGenerated -= OnGridGenerated;
        }

        private void Start()
        {
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

        private IEnumerator DelayedInit()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
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

            startPathNode = GetComponent<PathNode>()
                         ?? GetComponentInParent<PathNode>()
                         ?? GetPathNodeAtPosition(transform.position);

            if (startPathNode == null)
            {
                Debug.LogError($"EnemySpawner '{name}': No PathNode found at spawn position!");
                return;
            }

            if (!SetupEndNode())
            {
                Debug.LogError($"EnemySpawner '{name}': Could not find valid end node!");
                return;
            }

            isInitialized = true;

            if (showDebugLogs)
                Debug.Log($"EnemySpawner '{name}': Initialized → Target: {targetEndNode.name}");

            if (spawnOnStart)
                StartCoroutine(SpawnWaves());
        }

        private bool SetupEndNode()
        {
            if (Astar.Instance == null)
            {
                Debug.LogWarning("EnemySpawner: Astar.Instance is null!");
                return false;
            }

            if (assignedEndNode != null)
            {
                endPathNode = assignedEndNode.GetComponent<PathNode>()
                           ?? GetPathNodeAtPosition(assignedEndNode.position);

                if (endPathNode != null && IsPathValid(startPathNode, endPathNode))
                {
                    targetEndNode = assignedEndNode;
                    return true;
                }

                Debug.LogWarning($"EnemySpawner: Assigned end node '{assignedEndNode.name}' not reachable, finding closest...");
            }

            return FindClosestEndNode();
        }

        private bool FindClosestEndNode()
        {
            if (Astar.Instance == null) return false;

            if (!NodeGetter.nodeValue.ContainsKey(NodeType.End) ||
                NodeGetter.nodeValue[NodeType.End].Count == 0)
            {
                Debug.LogError("EnemySpawner: No end nodes registered in NodeGetter!");
                return false;
            }

            PathNode closestNode = null;
            float shortestDistance = float.MaxValue;

            foreach (PathNode endNode in NodeGetter.nodeValue[NodeType.End])
            {
                if (endNode == null) continue;

                List<PathNode> path = Astar.Instance.GetPath(startPathNode, endNode);
                if (path == null || path.Count == 0) continue;

                float distance = CalculatePathDistance(path);
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
                total += Vector3.Distance(path[i].transform.position, path[i + 1].transform.position);
            return total;
        }

        private PathNode GetPathNodeAtPosition(Vector3 position)
        {
            return PathNodeGenerator.Instance != null
                ? PathNodeGenerator.Instance.GetNodeAtWorldPosition(position)
                : null;
        }

        #endregion

        #region Wave Spawning

        /// <summary>
        /// Returns the WaveConfig for the given 1-based wave number.
        /// Clamps to the last defined wave when out of range.
        /// </summary>
        private WaveConfig GetConfigForWave(int waveNumber)
        {
            if (waves == null || waves.Count == 0)
            {
                // Fallback: single default wave
                return new WaveConfig();
            }

            int index = Mathf.Clamp(waveNumber - 1, 0, waves.Count - 1);
            return waves[index];
        }

        private IEnumerator SpawnWaves()
        {
            isSpawning = true;

            while (true)
            {
                WaveConfig config = GetConfigForWave(currentWave);

                if (showDebugLogs)
                    Debug.Log($"=== Wave {currentWave} starting: {config.enemyCount}x '{config.enemyPoolName}' ===");

                OnWaveStarted?.Invoke(currentWave);

                yield return StartCoroutine(SpawnWaveEnemies(config));

                // All enemies for this wave have been spawned
                OnWaveCompleted?.Invoke(currentWave);

                // Fire OnAllWavesCleared the first time the last defined wave finishes
                if (!allWavesCleared && waves != null && currentWave >= waves.Count)
                {
                    allWavesCleared = true;
                    OnAllWavesCleared?.Invoke(waves.Count);

                    if (showDebugLogs)
                        Debug.Log($"=== All {waves.Count} defined waves cleared! ===");
                }

                if (showDebugLogs)
                    Debug.Log($"=== Wave {currentWave} finished. Next wave in {config.cooldownAfter}s ===");

                OnWaveCooldownStarted?.Invoke(currentWave, config.cooldownAfter);

                yield return new WaitForSeconds(config.cooldownAfter);

                currentWave++;
            }
        }

        private IEnumerator SpawnWaveEnemies(WaveConfig config)
        {
            for (int i = 0; i < config.enemyCount; i++)
            {
                SpawnEnemy(i, config);

                if (i < config.enemyCount - 1)
                    yield return new WaitForSeconds(config.spawnInterval);
            }
        }

        private void SpawnEnemy(int index, WaveConfig config)
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogError("EnemySpawner: PoolManager.Instance is null!");
                return;
            }

            GameObject spawnedEnemy = PoolManager.Instance.Spawn(
                config.enemyPoolName,
                transform.position,
                Quaternion.identity
            );

            if (spawnedEnemy != null)
            {
                if (showDebugLogs)
                    Debug.Log($"Enemy {index + 1}/{config.enemyCount} spawned (Wave {currentWave}) → {targetEndNode.name}");

                MovementComponent movementComp = spawnedEnemy.GetComponent<MovementComponent>();
                if (movementComp != null)
                {
                    movementComp.targetTransform = targetEndNode;
                    movementComp.OnTriggerMove();
                }
                else
                {
                    Debug.LogWarning("EnemySpawner: Spawned enemy has no MovementComponent!");
                }
            }
            else
            {
                Debug.LogError($"EnemySpawner: Failed to spawn from pool '{config.enemyPoolName}'!");
            }
        }

        #endregion

        #region Public Methods

        public void TriggerSpawn()
        {
            if (!isInitialized) Init();
            if (!isSpawning && isInitialized)
                StartCoroutine(SpawnWaves());
        }

        public void StopSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
        }

        public void SetEndNode(Transform newEndNode)
        {
            assignedEndNode = newEndNode;
            if (isInitialized) SetupEndNode();
        }

        public void RecalculatePath()
        {
            if (!isInitialized) return;
            SetupEndNode();
        }

        public void Reinitialize()
        {
            isInitialized = false;
            Init();
        }

        /// <summary>
        /// Legacy configure method — populates Wave 1 for backwards compatibility.
        /// </summary>
        public void Configure(string poolName, int enemyCount, float interval, float cooldown, bool autoInit = true)
        {
            if (waves == null) waves = new List<WaveConfig>();
            if (waves.Count == 0) waves.Add(new WaveConfig());

            waves[0].enemyPoolName = poolName;
            waves[0].enemyCount    = enemyCount;
            waves[0].spawnInterval = interval;
            waves[0].cooldownAfter = cooldown;

            autoInitialize = autoInit;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = isInitialized ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "SPAWN");
#endif

            if (assignedEndNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                               assignedEndNode.position + Vector3.up * 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && targetEndNode != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                               targetEndNode.position + Vector3.up * 0.5f);
                Gizmos.DrawWireSphere(targetEndNode.position, 0.6f);

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
