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
}
