using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamage
{
    void Initialize(UnitSO data);
    void TryDealDamage(GameObject target);
}

public class DamageComponent : MonoBehaviour, IDamage
{
    private float damage;
    private DamageType damageType;


    public void Initialize(UnitSO data)
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
