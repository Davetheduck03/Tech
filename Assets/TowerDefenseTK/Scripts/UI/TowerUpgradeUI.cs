using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefenseTK
{

    /// <summary>
    /// Manages the tower upgrade UI panel
    /// </summary>
    public class TowerUpgradeUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Transform upgradeOptionsContainer;
        [SerializeField] private GameObject upgradeOptionPrefab;
        [SerializeField] private Button sellButton;
        [SerializeField] private TextMeshProUGUI sellButtonText;
        [SerializeField] private Button closeButton;

        [Header("Tower Info Display")]
        [SerializeField] private TextMeshProUGUI towerNameText;
        [SerializeField] private TextMeshProUGUI towerStatsText;
        [SerializeField] private Image towerIcon;

        private TowerUpgradeComponent selectedTower;
        private List<GameObject> spawnedUpgradeButtons = new List<GameObject>();

        private void Start()
        {
            HideUpgradePanel();

            // Setup sell button
            if (sellButton != null)
            {
                sellButton.onClick.AddListener(OnSellButtonClicked);
            }

            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideUpgradePanel);
            }
        }

        /// <summary>
        /// Show upgrade options for a selected tower
        /// </summary>
        public void ShowUpgradePanel(TowerUpgradeComponent tower)
        {
            if (tower == null) return;

            selectedTower = tower;
            upgradePanel.SetActive(true);

            // Display tower info
            DisplayTowerInfo(tower);

            // Clear previous upgrade options
            ClearUpgradeOptions();

            // Get and display available upgrades
            List<UpgradeOption> availableUpgrades = tower.GetAvailableUpgrades();

            if (availableUpgrades.Count == 0)
            {
                Debug.Log("No upgrades available for this tower");
            }
            else
            {
                foreach (var upgrade in availableUpgrades)
                {
                    CreateUpgradeButton(upgrade);
                }
            }

            // Setup sell button
            UpdateSellButton(tower);
        }

        private void DisplayTowerInfo(TowerUpgradeComponent tower)
        {
            TowerUnit towerUnit = tower.GetComponent<TowerUnit>();
            if (towerUnit == null || towerUnit.towerSO == null) return;

            if (towerNameText != null)
                towerNameText.text = towerUnit.towerSO.UnitName;

            if (towerStatsText != null)
            {
                towerStatsText.text = $"Range: {towerUnit.towerSO.range}\n" +
                                      $"Fire Rate: {towerUnit.towerSO.fireRate}\n" +
                                      $"Damage: {towerUnit.towerSO.damage}\n" +
                                      $"Kills: {tower.killCount}";
            }

            // Set tower icon if available
            if (towerIcon != null && towerUnit.towerSO.Icon != null)
            {
                towerIcon.sprite = towerUnit.towerSO.Icon;
            }
        }

        private void CreateUpgradeButton(UpgradeOption upgrade)
        {
            GameObject buttonObj = Instantiate(upgradeOptionPrefab, upgradeOptionsContainer);
            spawnedUpgradeButtons.Add(buttonObj);

            // Setup button components
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnUpgradeSelected(upgrade));
            }

            // Set button text/info
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = $"{upgrade.upgradedTower.UnitName}";
                texts[1].text = "Upgrade Cost: " + "$ " + upgrade.upgradeCost;
            }

            // Set upgrade icon if available
            Image icon = buttonObj.GetComponentInChildren<Image>();
            if (icon != null && upgrade.upgradedTower.Icon != null)
            {
                icon.sprite = upgrade.upgradedTower.Icon;
            }


            // Check if affordable
            bool canAfford = CurrencyManager.Instance.CurrentCurrency >= upgrade.upgradeCost;
            button.interactable = canAfford;

            if (!canAfford)
            {
                // Darken button or add visual feedback
                var colors = button.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f);
                button.colors = colors;
            }
        }

        private void UpdateSellButton(TowerUpgradeComponent tower)
        {
            if (sellButton == null || tower.upgradeData == null) return;

            sellButton.interactable = tower.upgradeData.canSell;

            if (sellButtonText != null && tower.upgradeData.canSell)
            {
                int sellValue = tower.upgradeData.GetSellValue();
                sellButtonText.text = $"Sell ({sellValue}g)";
            }
            else if (sellButtonText != null)
            {
                sellButtonText.text = "Cannot Sell";
            }
        }

        private void OnUpgradeSelected(UpgradeOption upgrade)
        {
            if (selectedTower == null) return;

            bool success = selectedTower.UpgradeTo(upgrade);

            if (success)
            {
                HideUpgradePanel();
            }
        }

        private void OnSellButtonClicked()
        {
            if (selectedTower == null) return;

            bool success = selectedTower.SellTower();

            if (success)
            {
                HideUpgradePanel();
            }
        }

        private void ClearUpgradeOptions()
        {
            foreach (var button in spawnedUpgradeButtons)
            {
                Destroy(button);
            }
            spawnedUpgradeButtons.Clear();
        }

        public void HideUpgradePanel()
        {
            upgradePanel.SetActive(false);
            selectedTower = null;
            ClearUpgradeOptions();
        }
    }
}
