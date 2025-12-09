using System.Collections;
using UnityEngine;

namespace TowerDefenseTK
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private string enemyPoolName = "Basic Enemy";
        [SerializeField] private int enemiesToSpawn = 5;
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private float waveCooldown = 10f;
        [SerializeField] private bool spawnOnStart = true;

        private int currentWave = 1;
        private bool isSpawning = false;

        public void Init()
        {
            if (spawnOnStart)
            {
                StartCoroutine(SpawnWaves());
            }
        }

        /// <summary>
        /// Main infinite wave loop
        /// </summary>
        private IEnumerator SpawnWaves()
        {
            isSpawning = true;

            while (true)
            {
                Debug.Log($"--- Wave {currentWave} starting ---");

                yield return StartCoroutine(SpawnWaveEnemies());

                Debug.Log($"--- Wave {currentWave} finished. Waiting {waveCooldown}s ---");

                yield return new WaitForSeconds(waveCooldown);

                currentWave++;
            }
        }

        /// <summary>
        /// Spawn all enemies inside a single wave
        /// </summary>
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
            GameObject spawnedEnemy = PoolManager.Instance.Spawn(
                enemyPoolName,
                transform.position,
                Quaternion.identity
            );

            if (spawnedEnemy != null)
            {
                Debug.Log($"Enemy {index + 1}/{enemiesToSpawn} spawned in Wave {currentWave}");

                MovementComponent movementComp = spawnedEnemy.GetComponent<MovementComponent>();
                if (movementComp != null)
                {
                    movementComp.OnTriggerMove();
                }
            }
            else
            {
                Debug.LogError($"Failed to spawn enemy {index + 1} from pool!");
            }
        }

        /// <summary>
        /// Start waves manually from UI
        /// </summary>
        public void TriggerSpawn()
        {
            if (!isSpawning)
            {
                StartCoroutine(SpawnWaves());
            }
        }
    }
}
