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
}

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
