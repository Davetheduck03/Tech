using UnityEngine;
using TowerDefenseTK;

public class DamageComponent : UnitComponent
{
    private float damage;
    private DamageType damageType;

    // Cached reference to the kill tracker on this same tower unit
    private TowerDamageTracker killTracker;

    // On-hit status effect from the tower's SO (null = no effect)
    private StatusEffectSO onHitEffect;

    // Optional buff component on the same tower — scales damage at fire time
    private TowerBuffComponent buffComponent;

    protected override void OnInitialize()
    {
        damage     = data.damage;
        damageType = data.damageType;
        killTracker    = unit.GetComponent<TowerDamageTracker>();
        buffComponent  = unit.GetComponent<TowerBuffComponent>();

        // Only towers have a StatusEffectSO — safe cast, null for enemy units
        if (data is TowerSO towerSO)
            onHitEffect = towerSO.statusEffect;
    }

    public void TryDealDamage(GameObject target)
    {
        if (target.TryGetComponent<HealthComponent>(out HealthComponent health))
        {
            // Scale damage by any active buff multiplier (1.0 when no buff)
            float effectiveDamage = damage *
                (buffComponent != null ? buffComponent.DamageMultiplier : 1f);

            // Pass 'this' so HealthComponent can credit the kill back to this tower if it dies
            health.TakeDamage(effectiveDamage, damageType, this);
        }

        // Apply on-hit status effect if the tower has one configured and the target can receive it
        if (onHitEffect != null)
        {
            var statusComp = target.GetComponent<StatusEffectComponent>();
            statusComp?.Apply(onHitEffect, this);
        }
    }

    /// <summary>
    /// Called by HealthComponent when the target this component killed has died.
    /// </summary>
    public void NotifyKill()
    {
        killTracker?.OnEnemyKilled(0);
    }
}
