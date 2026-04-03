using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

public class HealthComponent : UnitComponent
{
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever an enemy dies. Args: the enemy's GameObject, the
    /// DamageComponent that dealt the killing blow (null if unknown).
    /// Subscribers can call GetComponent&lt;EnemyUnit&gt;() on the GameObject if
    /// they need the full unit reference.
    /// </summary>
    public static event System.Action<GameObject, DamageComponent> OnEnemyDied;

    // ─────────────────────────────────────────────────────────────────────────

    public float currentHealth;
    public float maxHealth { get; private set; }
    public bool isDamagable;

    // Tracks which DamageComponent dealt the killing blow so we can credit the tower
    private DamageComponent lastAttacker;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.red;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
    private float flashTimer;

    protected override void OnInitialize()
    {
        maxHealth = data.Health;
        currentHealth = maxHealth;
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

    public void TakeDamage(float damage, DamageType damageType, DamageComponent attacker = null)
    {
        if (!isDamagable) return;

        lastAttacker = attacker;

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
        // Award gold to the player
        if (CurrencyManager.Instance != null && data.goldReward > 0)
        {
            CurrencyManager.Instance.Add((int)data.goldReward);
            Debug.Log($"{gameObject.name} died — awarded {(int)data.goldReward} gold.");
        }

        // Credit the kill to whichever tower landed the killing blow
        lastAttacker?.NotifyKill();

        // Notify toolkit subscribers (enemies only — units with a gold reward are enemies)
        if (data is EnemySO)
            OnEnemyDied?.Invoke(gameObject, lastAttacker);

        PoolManager.Instance.Despawn(gameObject);
    }
}
