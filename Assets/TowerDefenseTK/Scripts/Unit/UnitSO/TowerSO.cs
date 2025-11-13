using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "TD Toolkit/Units/Tower")]
public class TowerSO : UnitSO
{
    public enum TowerType
    {
        Turret,
        AoE,
        Support,
        Resource
    }

    public enum TargetType
    {
        First,
        Last,
        Closest,
        Strongest,
        Weakest
    }

    [Header("Tower Stats")]
    public float fireRate;
    public float range;
    public bool isAoE;
    public float projectileSpeed;
    public TowerType towerType;
    public TargetType towerTargetType = TargetType.First;

    [Tooltip("Gold cost to build this tower.")]
    public int buildCost;

    List<BaseEnemy> enemyList;
    //only for pseudocode purposes, will implement in wave manager later.

    public BaseEnemy currentTarget;
    public float lastScanTime;
    public float scanInterval;

    public BaseUnit FindTarget(Vector3 agentPos, float range)
    {
        if(currentTarget != null && currentTarget.gameObject.activeInHierarchy && Vector3.Distance(currentTarget.transform.position, agentPos) <= range)
        {
            return currentTarget;
        }

        currentTarget = null;

        if(Time.time -  lastScanTime < scanInterval) return null;

        lastScanTime = Time.time;

        var enemies = GetEnemiesInRange(agentPos, range);
        if( enemies.Count == 0 ) return null;

        currentTarget = GetEnemy(agentPos, GetEnemiesInRange(agentPos, range));
        
        return currentTarget;
    }

    public List<BaseEnemy> GetEnemiesInRange(Vector3 agentPos, float range)
    {
        var hits = Physics.OverlapSphere(agentPos, range, LayerMask.GetMask("Enemy"));
        var enemyList = new List<BaseEnemy>(hits.Length);
        
        foreach(var hit in hits)
        {
            var enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemyList.Add(enemy);
            }
        }
        return enemyList;
    }

    public BaseEnemy GetEnemy(Vector3 agentPos, List<BaseEnemy> elegibleEnemies)
    {
        return towerTargetType switch
        { 
            TargetType.First => GetFirst(),
            TargetType.Last => GetLast(),
            TargetType.Weakest => GetWeakest(),
            TargetType.Strongest => GetStrongest(),
            TargetType.Closest => GetClostest(agentPos, elegibleEnemies)
        };
    }

    public BaseEnemy GetClostest(Vector3 agentPos, List<BaseEnemy> elegibleEnemies)
    {
        BaseEnemy closest = null;
        float closestDist = float.MaxValue;

        foreach(var enemy in elegibleEnemies)
        {
            float dist = Vector3.Distance(enemy.transform.position, agentPos);
            if(dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest; 
    }

    public BaseEnemy GetFirst()
    {
        return null;
    }

    public BaseEnemy GetStrongest()
    {

        return null;
    }

    public BaseEnemy GetWeakest()
    {
        return null;
    }

    public BaseEnemy GetLast()
    {
        return null;
    }



}
