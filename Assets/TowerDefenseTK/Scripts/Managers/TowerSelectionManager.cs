using UnityEngine;
using TowerDefenseTK;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Manages tower selection via raycasting.
/// Compatible with both the legacy Input Manager and the new Input System package.
/// Place this on a manager GameObject in your scene.
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

    // ── Input Helpers ─────────────────────────────────────────────────────────
#if ENABLE_INPUT_SYSTEM
    private static bool  LeftClickDown  => Mouse.current?.leftButton.wasPressedThisFrame  ?? false;
    private static bool  RightClickDown => Mouse.current?.rightButton.wasPressedThisFrame ?? false;
    private static Vector2 MousePos     => Mouse.current?.position.ReadValue() ?? Vector2.zero;
#else
    private static bool  LeftClickDown  => Input.GetMouseButtonDown(0);
    private static bool  RightClickDown => Input.GetMouseButtonDown(1);
    private static Vector2 MousePos     => Input.mousePosition;
#endif

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
            Debug.LogError("TowerSelectionManager: TowerUpgradeUI not found in scene!");
    }

    private void Update()
    {
        if (LeftClickDown && !IsInPlacementMode())
            TrySelectTower();

        if (RightClickDown)
            DeselectTower();
    }

    private bool IsInPlacementMode()
    {
        return TowerPlacementController.Instance != null &&
               TowerPlacementController.Instance.IsPlacing;
    }

    private void TrySelectTower()
    {
        Ray ray = Camera.main.ScreenPointToRay((Vector3)MousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, towerLayer))
        {
            TowerUpgradeComponent tower = hit.collider.GetComponentInParent<TowerUpgradeComponent>();
            if (tower != null)
                SelectTower(tower);
        }
    }

    /// <summary>Select a tower and show its upgrade UI</summary>
    public void SelectTower(TowerUpgradeComponent tower)
    {
        if (tower == null) return;

        selectedTower = tower;

        if (upgradeUI != null)
            upgradeUI.ShowUpgradePanel(tower);

        ShowSelectionIndicator(tower.transform.position);
        Debug.Log($"Selected tower: {tower.gameObject.name}");
    }

    /// <summary>Deselect current tower and hide UI</summary>
    public void DeselectTower()
    {
        selectedTower = null;

        if (upgradeUI != null)
            upgradeUI.HideUpgradePanel();

        HideSelectionIndicator();
        Debug.Log("Tower deselected");
    }

    private void ShowSelectionIndicator(Vector3 position)
    {
        if (selectionIndicator == null) return;

        if (currentIndicator == null)
            currentIndicator = Instantiate(selectionIndicator, position, Quaternion.identity);
        else
        {
            currentIndicator.transform.position = position;
            currentIndicator.SetActive(true);
        }
    }

    private void HideSelectionIndicator()
    {
        if (currentIndicator != null)
            currentIndicator.SetActive(false);
    }

    /// <summary>Get the currently selected tower</summary>
    public TowerUpgradeComponent GetSelectedTower() => selectedTower;

    private void OnDrawGizmos()
    {
        if (selectedTower != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(selectedTower.transform.position, 1f);
        }
    }
}
