using System.Collections;
using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class BaseEnemy : BaseUnit
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
}
