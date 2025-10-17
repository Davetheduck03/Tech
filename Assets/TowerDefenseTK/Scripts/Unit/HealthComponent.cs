using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHealth
{
    void Initialize(UnitSO data);
    void TakeDamage(DamageData damageData);
}

public class HealthComponent : MonoBehaviour, IHealth
{
    private float currentHealth;
    public bool isDamagable;

    public void Initialize(UnitSO data)
    {
        currentHealth = data.Health;
    }

    public void TakeDamage(DamageData data)
    {

    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
