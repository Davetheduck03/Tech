using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageComponent : UnitComponent
{
    private float damage;
    private DamageType damageType;


    protected override void OnInitialize()
    {
        damage = data.damage;
        damageType = data.damageType;
    }

    public void TryDealDamage(GameObject target)
    {
        if (target.TryGetComponent<HealthComponent>(out HealthComponent health))
        {
            if (health.isDamagable)
            {
                DamageData damageData = new DamageData(damage, damageType, this.gameObject);
                health.TakeDamage(damageData);
            }
            else return;
        }
    }
}
