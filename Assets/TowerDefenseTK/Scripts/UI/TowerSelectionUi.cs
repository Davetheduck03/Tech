using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    [SerializeField] private TowerDatabase database;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;
    private TowerSO currentTower;

    void Start()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (TowerSO tower in database.towers)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            TowerUIButton button = btnObj.GetComponent<TowerUIButton>();
            button.Init(tower, this);
        }
    }

    public void SelectTower(TowerSO tower)
    {
        TowerPlacementController.Instance.SetTowerToPlace(tower);
    }
}