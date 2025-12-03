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

    public TowerType towerType;
    public TargetType towerTargetType = TargetType.First;
    public TargetGroup targetGroup;
    public AOEType AOEType;
    public float projectileSpeed;
    public float AOE_Radius;
 
    [Tooltip("Gold cost to build this tower.")]
    public int buildCost;

    public GameObject previewPrefab;
}

public enum TowerType
{
    Turret,
    AoE,
    Support,
    Resource
}

public enum AOEType
{
    Cone,
    Circle,
    Turret_AOE
}
//Only used for AOE Towers

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
