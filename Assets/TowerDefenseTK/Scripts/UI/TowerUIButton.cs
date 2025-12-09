using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefenseTK
{

    public class TowerUIButton : MonoBehaviour
    {
        private Button button;
        private UnitSO towerData;
        private TowerSelectionUI selectionUI;
        [SerializeField] private TMP_Text costText;

        public void Init(UnitSO data, TowerSelectionUI ui)
        {
            button = this.GetComponent<Button>();
            TowerSO towerSpecificData = (TowerSO)data;
            towerData = data;
            button.image.sprite = towerData.Icon;
            selectionUI = ui;
            costText.text = towerSpecificData.buildCost.ToString();
        }

        public void OnClick()
        {
            selectionUI.SelectTower((TowerSO)towerData);
        }
    }
}
