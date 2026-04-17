using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Tracks damage dealt and kills for upgrade requirements.
    /// Inherits from UnitComponent to follow the component architecture.
    /// </summary>
    public class TowerDamageTracker : UnitComponent
    {
        private TowerUpgradeComponent upgradeComponent;

        protected override void OnInitialize()
        {
            upgradeComponent = unit.GetComponent<TowerUpgradeComponent>();

            if (upgradeComponent == null)
                Debug.LogWarning("TowerDamageTracker: No TowerUpgradeComponent found on this unit. Add one if you want kill/damage tracking.");
        }

        /// <summary>Call this when an enemy is killed.</summary>
        public void OnEnemyKilled(int damageDealt)
        {
            if (upgradeComponent == null) return;
            upgradeComponent.killCount++;
            upgradeComponent.totalDamageDealt += damageDealt;
            Debug.Log($"Tower kill count: {upgradeComponent.killCount}");
        }

        /// <summary>Call this when damage is dealt (even if not a kill).</summary>
        public void OnDamageDealt(int damageAmount)
        {
            if (upgradeComponent == null) return;
            upgradeComponent.totalDamageDealt += damageAmount;
        }
    }
}
