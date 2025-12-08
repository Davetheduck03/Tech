using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Add this component to your TowerUnit to handle upgrades
/// Inherits from UnitComponent to follow your component architecture
/// </summary>
public class TowerUpgradeComponent : UnitComponent
{
    [Header("Upgrade Settings")]
    public TowerUpgradeData upgradeData;
    
    [Header("Runtime Stats")]
    public int killCount = 0;
    public int totalDamageDealt = 0;
    
    private TowerSO towerData;
    private GridManager gridManager;
    private Vector2Int gridPosition;
    
    protected override void OnInitialize()
    {
        // Cast the data to TowerSO
        if (data is TowerSO tower)
        {
            towerData = tower;
        }
        else
        {
            Debug.LogError("TowerUpgradeComponent requires TowerSO data!");
            return;
        }
        
        // Initialize grid manager and position
        gridManager = GridManager.Instance;
        
        if (gridManager != null)
        {
            gridPosition = gridManager.WorldToGrid(transform.position);
        }
    }
    
    /// <summary>
    /// Get all available upgrades for this tower
    /// </summary>
    public List<UpgradeOption> GetAvailableUpgrades()
    {
        if (upgradeData == null || upgradeData.upgradeOptions.Count == 0)
            return new List<UpgradeOption>();
        
        List<UpgradeOption> available = new List<UpgradeOption>();
        int currentGold = CurrencyManager.Instance.CurrentCurrency;
        
        foreach (var option in upgradeData.upgradeOptions)
        {
            if (upgradeData.IsUpgradeAvailable(option, currentGold, 0, killCount, 0))
            {
                available.Add(option);
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// Upgrade this tower to a new tower type
    /// </summary>
    public bool UpgradeTo(UpgradeOption upgradeOption)
    {
        if (upgradeOption == null || upgradeOption.upgradedTower == null)
        {
            Debug.LogError("Invalid upgrade option!");
            return false;
        }
        
        // Check if we can afford it
        if (!CurrencyManager.Instance.TrySpend(upgradeOption.upgradeCost))
        {
            Debug.Log($"Not enough gold to upgrade! Need {upgradeOption.upgradeCost}");
            return false;
        }
        
        // Store the current position and rotation
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        
        // Mark the cell as unoccupied temporarily
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, false);
        }
        
        // Instantiate the upgraded tower (this is the full TowerUnit prefab)
        GameObject upgradedTowerObj = Instantiate(
            upgradeOption.upgradedTower.UnitPrefab, 
            position, 
            rotation
        );
        
        // Transfer stats if needed (optional)
        TowerUpgradeComponent upgradedComponent = upgradedTowerObj.GetComponent<TowerUpgradeComponent>();
        if (upgradedComponent != null)
        {
            upgradedComponent.killCount = this.killCount;
            upgradedComponent.totalDamageDealt = this.totalDamageDealt;
        }
        
        // Mark the cell as occupied again
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, true);
        }
        
        // Play upgrade effects (optional)
        PlayUpgradeEffect();
        
        // Destroy the old tower
        Destroy(gameObject);
        
        Debug.Log($"Tower upgraded to {upgradeOption.upgradedTower.UnitName}!");
        return true;
    }
    
    /// <summary>
    /// Sell this tower and get gold back
    /// </summary>
    public bool SellTower()
    {
        if (upgradeData == null || !upgradeData.canSell)
        {
            Debug.Log("This tower cannot be sold!");
            return false;
        }
        
        int sellValue = upgradeData.GetSellValue();
        
        // Give gold back
        CurrencyManager.Instance.Add(sellValue);
        
        // Mark cell as unoccupied
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, false);
        }
        
        // Play sell effect (optional)
        PlaySellEffect();
        
        Debug.Log($"Tower sold for {sellValue} gold!");
        Destroy(gameObject);
        return true;
    }
    
    /// <summary>
    /// Call this when the tower kills an enemy
    /// </summary>
    public void OnEnemyKilled(int damageDealt)
    {
        killCount++;
        totalDamageDealt += damageDealt;
    }
    
    private void PlayUpgradeEffect()
    {
        // TODO: Add particle effects, sounds, etc.
        // Example: Instantiate(upgradeVFX, transform.position, Quaternion.identity);
    }
    
    private void PlaySellEffect()
    {
        // TODO: Add particle effects, sounds, etc.
        // Example: Instantiate(sellVFX, transform.position, Quaternion.identity);
    }
}
