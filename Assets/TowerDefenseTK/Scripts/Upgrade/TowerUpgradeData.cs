using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{

    [System.Serializable]
    public class UpgradeOption
    {
        [Tooltip("The tower this can upgrade into")]
        public TowerSO upgradedTower;

        [Tooltip("Gold cost to perform this upgrade")]
        public int upgradeCost;

        [Tooltip("Optional: Requirements to unlock this upgrade")]
        public List<UpgradeRequirement> requirements = new List<UpgradeRequirement>();
    }

    [System.Serializable]
    public class UpgradeRequirement
    {
        public RequirementType type;
        public int value;

        public enum RequirementType
        {
            PlayerLevel,
            TowerKills,
            RoundNumber
        }
    }

    /// <summary>
    /// Add this component to your TowerSO to define upgrade paths
    /// </summary>
    [CreateAssetMenu(fileName = "New Tower Upgrade", menuName = "TD Toolkit/Tower Upgrade")]
    public class TowerUpgradeData : ScriptableObject
    {
        [Tooltip("The base tower that can be upgraded")]
        public TowerSO baseTower;

        [Tooltip("Available upgrade options for this tower")]
        public List<UpgradeOption> upgradeOptions = new List<UpgradeOption>();

        [Tooltip("Can this tower be sold back?")]
        public bool canSell = true;

        [Tooltip("Percentage of build cost returned when sold (0-100)")]
        [Range(0, 100)]
        public int sellValuePercent = 75;

        /// <summary>
        /// Check if a specific upgrade is available
        /// </summary>
        public bool IsUpgradeAvailable(UpgradeOption upgrade, int currentGold, int playerLevel = 0, int towerKills = 0, int currentRound = 0)
        {
            // Check gold
            if (currentGold < upgrade.upgradeCost)
                return false;

            // Check requirements
            foreach (var req in upgrade.requirements)
            {
                switch (req.type)
                {
                    case UpgradeRequirement.RequirementType.PlayerLevel:
                        if (playerLevel < req.value) return false;
                        break;
                    case UpgradeRequirement.RequirementType.TowerKills:
                        if (towerKills < req.value) return false;
                        break;
                    case UpgradeRequirement.RequirementType.RoundNumber:
                        if (currentRound < req.value) return false;
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the sell value for this tower
        /// </summary>
        public int GetSellValue()
        {
            if (!canSell) return 0;
            return Mathf.RoundToInt(baseTower.buildCost * (sellValuePercent / 100f));
        }
    }
}
