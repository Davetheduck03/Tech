using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class HealthComponent : UnitComponent
{
    public float currentHealth;
    public bool isDamagable;

    protected override void OnInitialize()
    {
        currentHealth = data.Health;
        isDamagable = true;
    }

    public void TakeDamage(DamageData data)
    {
        if (!isDamagable) return;

        float baseAmount = data.amount;
        float finalDamage = this.data.CalculateDamageTaken(baseAmount, data.damageType);
        currentHealth -= finalDamage;

        Debug.Log($"{gameObject.name} took {finalDamage} {data.damageType} damage. Remaining HP: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }

    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
