using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Add this to track damage and kills for upgrade requirements
/// Inherits from UnitComponent to follow your component architecture
/// </summary>
public class TowerDamageTracker : UnitComponent
{
    private TowerUpgradeComponent upgradeComponent;
    
    protected override void OnInitialize()
    {
        // Find the upgrade component on the same BaseUnit
        upgradeComponent = unit.GetComponent<TowerUpgradeComponent>();
        
        if (upgradeComponent == null)
        {
            Debug.LogWarning("TowerDamageTracker: No TowerUpgradeComponent found on this unit!");
        }
    }
    
    /// <summary>
    /// Call this when an enemy is killed
    /// </summary>
    public void OnEnemyKilled(int damageDealt)
    {
        if (upgradeComponent != null)
        {
            upgradeComponent.killCount++;
            upgradeComponent.totalDamageDealt += damageDealt;
            
            Debug.Log($"Tower kill count: {upgradeComponent.killCount}");
        }
    }
    
    /// <summary>
    /// Call this when damage is dealt (even if not a kill)
    /// </summary>
    public void OnDamageDealt(int damageAmount)
    {
        if (upgradeComponent != null)
        {
            upgradeComponent.totalDamageDealt += damageAmount;
        }
    }
}
