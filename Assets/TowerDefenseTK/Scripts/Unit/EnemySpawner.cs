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
        [SerializeField] private bool spawnOnStart = true;

        void Start()
        {
            if (spawnOnStart)
            {
                StartCoroutine(SpawnEnemies());
            }
        }

        private IEnumerator SpawnEnemies()
        {
            Debug.Log($"Starting to spawn {enemiesToSpawn} enemies with {spawnInterval}s interval");

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy(i);

                // Wait before spawning the next enemy (except after the last one)
                if (i < enemiesToSpawn - 1)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            Debug.Log($"Finished spawning {enemiesToSpawn} enemies");
        }

        private void SpawnEnemy(int index)
        {
            GameObject spawnedEnemy = PoolManager.Instance.Spawn(enemyPoolName, transform.position, Quaternion.identity);

            if (spawnedEnemy != null)
            {
                Debug.Log($"Enemy {index + 1}/{enemiesToSpawn} spawned from pool");

                // Trigger movement if the enemy has a MovementComponent
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
        /// Manually trigger spawning (can be called from buttons, etc.)
        /// </summary>
        public void TriggerSpawn()
        {
            StartCoroutine(SpawnEnemies());
        }

        /// <summary>
        /// Spawn a custom number of enemies
        /// </summary>
        public void SpawnCustomAmount(int amount)
        {
            StartCoroutine(SpawnCustomEnemies(amount));
        }

        private IEnumerator SpawnCustomEnemies(int amount)
        {
            Debug.Log($"Starting to spawn {amount} enemies with {spawnInterval}s interval");

            for (int i = 0; i < amount; i++)
            {
                SpawnEnemy(i);

                if (i < amount - 1)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            Debug.Log($"Finished spawning {amount} enemies");
        }
    }
}