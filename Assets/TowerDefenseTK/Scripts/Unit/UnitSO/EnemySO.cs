using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "TD Toolkit/Units/Enemy")]
public class EnemySO : UnitSO
{
    public bool isFlying;

    [Header("Base Damage")]
    [Tooltip("Damage dealt to player when enemy reaches the exit")]
    public int damageToBase = 1;

    [Header("Tower Combat")]
    [Tooltip("If true, this enemy will attack towers it walks past. Requires an EnemyWeapon component on the prefab.")]
    public bool canAttackTowers = false;

    [Tooltip("Range within which this enemy can target and attack towers.")]
    public float attackRange = 3f;

    [Tooltip("Attacks per second against towers.")]
    public float attackRate = 1f;
}
