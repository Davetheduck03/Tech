using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    public UnitSO unitData;

    private HealthComponent healthComponent;
    private DamageComponent damageComponent;
    private MovementComponent movementComponent;

    protected virtual void Init()
    {
        healthComponent = GetComponent<HealthComponent>();
        damageComponent = GetComponent<DamageComponent>();
        movementComponent = GetComponent<MovementComponent>();


    }
}
