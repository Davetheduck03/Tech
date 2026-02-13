using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Attach to Exit/End nodes. When enemies reach this zone:
    /// 1. Deals damage to player
    /// 2. Returns enemy to pool
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int defaultDamage = 1;
        [SerializeField] private bool useEnemyDamageValue = true;

        [Header("Debug")]
        [SerializeField] private bool logDamage = true;

        private void Awake()
        {
            // Ensure collider is trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's an enemy
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();
            if (enemy == null)
            {
                enemy = other.GetComponentInParent<BaseEnemy>();
            }

            if (enemy == null) return;

            // Calculate damage
            int damage = defaultDamage;

            if (useEnemyDamageValue)
            {
                // Try to get damage from EnemySO
                EnemySO enemyData = enemy.GetEnemyData();
                if (enemyData != null)
                {
                    damage = enemyData.damageToBase;
                }
            }

            // Deal damage to player
            if (PlayerHealthManager.Instance != null)
            {
                PlayerHealthManager.Instance.TakeDamage(damage);
                
                if (logDamage)
                    Debug.Log($"ExitZone: Enemy '{enemy.name}' reached exit! Player takes {damage} damage.");
            }

            // Return enemy to pool
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(enemy.gameObject);
            }
            else
            {
                // Fallback: destroy if no pool manager
                Destroy(enemy.gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    Gizmos.DrawSphere(capsule.center, capsule.radius);
                }
            }
        }
    }
}
