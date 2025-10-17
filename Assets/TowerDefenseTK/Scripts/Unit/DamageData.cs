using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DamageData
{
    public float amount;
    public DamageType damageType;
    public GameObject damageDealer;

    public DamageData(float amount, DamageType damageType, GameObject damageDealer)
    {
        this.amount = amount;
        this.damageType = damageType;
        this.damageDealer = damageDealer;
    }

}
