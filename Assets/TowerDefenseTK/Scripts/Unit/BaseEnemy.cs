using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseEnemy : BaseUnit, IPoolable
{
    [HideInInspector] public int nodesPassed;      
    [HideInInspector] public int totalPathNodes;   
    [HideInInspector] public bool isFlying;        

    private void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
    }

    private void OnDestroy()
    {
        EnemyManager.Instance.UnregisterEnemy(this);
    }

    public void OnSpawned()
    {
        throw new System.NotImplementedException();
    }

    public void OnDespawned()
    {
        throw new System.NotImplementedException();
    }
}
