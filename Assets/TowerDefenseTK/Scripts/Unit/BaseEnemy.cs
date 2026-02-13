using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseEnemy : BaseUnit, IPoolable
{
    [HideInInspector] public int nodesPassed;
    [HideInInspector] public int totalPathNodes;
    [HideInInspector] public bool isFlying;

    /// <summary>
    /// Get the EnemySO data for this enemy
    /// </summary>
    public EnemySO GetEnemyData()
    {
        // Access unitData directly - we inherit from BaseUnit so we have access
        return unitData as EnemySO;
    }

    public void OnSpawned()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    public void OnDespawned()
    {
        EnemyManager.Instance.UnregisterEnemy(this);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("End"))
        {
            // Deal damage to player
            if (PlayerHealthManager.Instance != null)
            {
                EnemySO enemyData = GetEnemyData();
                int damage = enemyData != null ? enemyData.damageToBase : 1;
                PlayerHealthManager.Instance.TakeDamage(damage);

                Debug.Log($"Enemy '{name}' reached exit! Player takes {damage} damage.");
            }

            // Return to pool
            PoolManager.Instance.Despawn(gameObject);
        }
    }
}