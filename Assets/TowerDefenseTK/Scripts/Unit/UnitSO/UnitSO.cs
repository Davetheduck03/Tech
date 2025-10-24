using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public abstract class UnitSO : ScriptableObject
{

    [Header("Basic Info")]
    public string UnitName;
    public GameObject UnitPrefab;
    public Sprite Icon;

    [Header("Base Stats")]
    public float Health;
    public float Speed;
    public float goldReward;
    public float damage;

    [Header("Attack Info")]
    public DamageType damageType;

    [Header("Defense Info")]
    public List<DefenseType> defenseTypes = new List<DefenseType>();


    [Header("Optional Stats")]
    public float armor;

    /// <summary>
    /// Returns the damage after applying the damage table multipliers
    /// </summary>
    public float CalculateDamageTaken(float baseAmount, DamageType incomingType)
    {
        float result = baseAmount;

        if (SOManager.Instance.DamageTable != null && defenseTypes != null && defenseTypes.Count > 0)
        {
            foreach (var defType in defenseTypes)
            {
                result *= SOManager.Instance.DamageTable.GetMultiplier(incomingType, defType);
            }
        }

        return result;
    }
}
