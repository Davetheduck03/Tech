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

    public void TakeDamage(float damage, DamageType damageType)
    {
        if (!isDamagable) return;

        currentHealth -= data.CalculateDamageTaken(damage, damageType);

        Debug.Log($"{gameObject.name} took {data.CalculateDamageTaken(damage, damageType)} {damageType} damage. Remaining HP: {currentHealth}");
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
