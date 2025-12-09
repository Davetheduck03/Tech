using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

public class HealthComponent : UnitComponent
{
    public float currentHealth;
    public bool isDamagable;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.red;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
    private float flashTimer;

    protected override void OnInitialize()
    {
        currentHealth = data.Health;
        isDamagable = true;

        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        originalColor = Color.white;
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;

            float t = flashTimer / flashDuration;
            Color currentColor = Color.Lerp(originalColor, flashColor, t);

            propertyBlock.SetColor("_Color", currentColor);
            propertyBlock.SetColor("_BaseColor", currentColor); // For URP/HDRP

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }
    }

    public void TakeDamage(float damage, DamageType damageType)
    {
        if (!isDamagable) return;

        currentHealth -= data.CalculateDamageTaken(damage, damageType);
        Debug.Log($"{gameObject.name} took {data.CalculateDamageTaken(damage, damageType)} {damageType} damage. Remaining HP: {currentHealth}");

        // Trigger flash
        flashTimer = flashDuration;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        PoolManager.Instance.Despawn(gameObject);
    }
}