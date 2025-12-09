using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseEnemy : BaseUnit, IPoolable
{
    [HideInInspector] public int nodesPassed;
    [HideInInspector] public int totalPathNodes;
    [HideInInspector] public bool isFlying;


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
            PoolManager.Instance.Despawn(gameObject);
        }
    }
}
