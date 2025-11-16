using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using Unity.VisualScripting;

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

    public enum TargetGroup
    {
        Ground,
        Air,
        Both
    }

    [Header("Tower Stats")]
    public float fireRate;
    public float range;
    public bool isAoE;
    public float projectileSpeed;
    public TowerType towerType;
    public TargetType towerTargetType = TargetType.First;
    public TargetGroup targetGroup;

    [Tooltip("Gold cost to build this tower.")]
    public int buildCost;

    List<BaseEnemy> enemyList;
    //only for pseudocode purposes, will implement in wave manager later.

    public BaseEnemy currentTarget;
    public float lastScanTime;
    public float scanInterval;

    public BaseEnemy FindTarget(Vector3 agentPos, float range)
    {
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy && Vector3.Distance(currentTarget.transform.position, agentPos) <= range)
        {
            return currentTarget;
        }

        currentTarget = null;

        if (Time.time - lastScanTime < scanInterval) return null;

        lastScanTime = Time.time;

        var enemies = GetEnemiesInRange(agentPos, range);
        if (enemies.Count == 0) return null;

        currentTarget = GetEnemy(agentPos, GetEnemiesInRange(agentPos, range));
        return currentTarget;
    }

    public List<BaseEnemy> GetEnemiesInRange(Vector3 agentPos, float range)
    {
        var hits = Physics.OverlapSphere(agentPos, range, LayerMask.GetMask("Enemy"));
        var enemyList = new List<BaseEnemy>(hits.Length);

        Debug.Log("Enemies found in vicinity, amount: " + hits.Length);

        foreach (var hit in hits)
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
            TargetType.First => GetFirst(elegibleEnemies),
            TargetType.Last => GetLast(elegibleEnemies),
            TargetType.Weakest => GetWeakest(elegibleEnemies),
            TargetType.Strongest => GetStrongest(elegibleEnemies),
            TargetType.Closest => GetClostest(agentPos, elegibleEnemies),
            _ => throw new System.NotImplementedException($"Target type {towerTargetType} is not supported.")
        };
    }

    public BaseEnemy GetClostest(Vector3 agentPos, List<BaseEnemy> elegibleEnemies)
    {
        BaseEnemy closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in elegibleEnemies)
        {
            float dist = Vector3.Distance(enemy.transform.position, agentPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    public BaseEnemy GetFirst(List<BaseEnemy> elegibleEnemies)
    {
        BaseEnemy first = elegibleEnemies[0];
        return first;
    }

    public BaseEnemy GetStrongest(List<BaseEnemy> elegibleEnemies)
    {
        //Get highest current health enemy
        BaseEnemy highestHealth = null;
        float highest = 0f;
        foreach (BaseEnemy enemy in elegibleEnemies)
        {
            var healthComponent = enemy.gameObject.GetComponent<HealthComponent>();
            if (healthComponent.currentHealth >= highest)
            {
                highest = healthComponent.currentHealth;
                highestHealth = enemy;
            }
        }
        return highestHealth;
    }

    public BaseEnemy GetWeakest(List<BaseEnemy> elegibleEnemies)
    {
        //Get lowest current health enemy
        BaseEnemy lowestHealth = null;
        float lowest = 0f;
        foreach (BaseEnemy enemy in elegibleEnemies)
        {
            var healthComponent = enemy.gameObject.GetComponent<HealthComponent>();
            if (healthComponent.currentHealth <= lowest)
            {
                lowest = healthComponent.currentHealth;
                lowestHealth = enemy;
            }
        }
        return lowestHealth;
    }

    public BaseEnemy GetLast(List<BaseEnemy> elegibleEnemies)
    {
        BaseEnemy last = elegibleEnemies.Last();
        return last;
    }
}
