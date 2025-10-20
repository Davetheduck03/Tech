using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "TD Toolkit/Units/Tower")]
public class TowerSO : UnitSO
{

    [Header("Tower Stats")]
    public float fireRate;
    public float range;
    public bool isAoE;
    public float projectileSpeed;

    [Tooltip("Gold cost to build this tower.")]
    public int buildCost;
}
