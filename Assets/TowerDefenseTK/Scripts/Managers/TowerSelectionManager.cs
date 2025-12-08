using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Manages tower selection via raycasting
/// Place this on a manager GameObject in your scene
/// </summary>
public class TowerSelectionManager : MonoBehaviour
{
    public static TowerSelectionManager Instance;
    
    [Header("Selection Settings")]
    [SerializeField] private LayerMask towerLayer;
    [SerializeField] private float maxRaycastDistance = 100f;
    
    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject selectionIndicator;
    
    private TowerUpgradeComponent selectedTower;
    private GameObject currentIndicator;
    private TowerUpgradeUI upgradeUI;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        upgradeUI = FindAnyObjectByType<TowerUpgradeUI>();
        
        if (upgradeUI == null)
        {
            Debug.LogError("TowerSelectionManager: TowerUpgradeUI not found in scene!");
        }
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !IsInPlacementMode())
        {
            TrySelectTower();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            DeselectTower();
        }
    }
    
    private bool IsInPlacementMode()
    {
        if (TowerPlacementController.Instance != null)
        {
            return TowerPlacementController.Instance.IsPlacingTower;
        }
        return false;
    }
    
    private void TrySelectTower()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, towerLayer))
        {

            TowerUpgradeComponent tower = hit.collider.GetComponentInParent<TowerUpgradeComponent>();
            if (tower != null)
            {
                SelectTower(tower);
            }
        }
    }
    
    /// <summary>
    /// Select a tower and show its upgrade UI
    /// </summary>
    public void SelectTower(TowerUpgradeComponent tower)
    {
        if (tower == null) return;
        
        selectedTower = tower;
        
        // Show upgrade UI
        if (upgradeUI != null)
        {
            upgradeUI.ShowUpgradePanel(tower);
        }
        
        // Show visual indicator (optional)
        ShowSelectionIndicator(tower.transform.position);
        
        Debug.Log($"Selected tower: {tower.gameObject.name}");
    }
    
    /// <summary>
    /// Deselect current tower and hide UI
    /// </summary>
    public void DeselectTower()
    {
        selectedTower = null;
        
        if (upgradeUI != null)
        {
            upgradeUI.HideUpgradePanel();
        }
        
        HideSelectionIndicator();
        
        Debug.Log("Tower deselected");
    }
    
    private void ShowSelectionIndicator(Vector3 position)
    {
        if (selectionIndicator == null) return;
        
        if (currentIndicator == null)
        {
            currentIndicator = Instantiate(selectionIndicator, position, Quaternion.identity);
        }
        else
        {
            currentIndicator.transform.position = position;
            currentIndicator.SetActive(true);
        }
    }
    
    private void HideSelectionIndicator()
    {
        if (currentIndicator != null)
        {
            currentIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get the currently selected tower
    /// </summary>
    public TowerUpgradeComponent GetSelectedTower()
    {
        return selectedTower;
    }
    
    private void OnDrawGizmos()
    {
        if (selectedTower != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(selectedTower.transform.position, 1f);
        }
    }
}
