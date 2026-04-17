using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TowerDefenseTK
{

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

        [Header("Range Indicator")]
        [Tooltip("Colour of the ring shown around a selected tower.")]
        [SerializeField] private Color rangeIndicatorColor = new Color(0f, 0.85f, 1f, 0.7f); // cyan

        private TowerUpgradeComponent selectedTower;
        private GameObject  currentIndicator;
        private TowerUpgradeUI upgradeUI;
        private RangeIndicator rangeIndicator;

        /// <summary>True while a tower is selected.</summary>
        public bool HasSelection => selectedTower != null;

        // ── Input Helpers ─────────────────────────────────────────────────────────
#if ENABLE_INPUT_SYSTEM
    private static bool  LeftClickDown  => Mouse.current?.leftButton.wasPressedThisFrame  ?? false;
    private static bool  RightClickDown => Mouse.current?.rightButton.wasPressedThisFrame ?? false;
    private static Vector2 MousePos     => Mouse.current?.position.ReadValue() ?? Vector2.zero;
#else
        private static bool LeftClickDown => Input.GetMouseButtonDown(0);
        private static bool RightClickDown => Input.GetMouseButtonDown(1);
        private static Vector2 MousePos => Input.mousePosition;
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

            // Refresh range ring radius every frame so buff changes are reflected live
            if (rangeIndicator != null && selectedTower != null)
            {
                var unit = selectedTower.GetComponent<TowerUnit>();
                if (unit != null)
                    rangeIndicator.SetRadius(unit.EffectiveRange);
            }
        }

        private bool IsInPlacementMode()
        {
            if (TowerPlacementController.Instance == null) return false;
            // ClickConsumed stays true for the rest of the frame even if placement
            // mode ended mid-frame (e.g. exitAfterPlace=true), preventing the same
            // click from also triggering tower selection.
            return TowerPlacementController.Instance.IsPlacing ||
                   TowerPlacementController.Instance.ClickConsumed;
        }

        private void TrySelectTower()
        {
            // Don't raycast into the world when the cursor is over a UI element.
            // This prevents upgrade buttons from selecting a tower behind the panel.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Camera.main == null) { Debug.LogWarning("[TowerSelectionManager] No MainCamera found. Tag your camera as MainCamera."); return; }
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

            // Attach a range ring to the selected tower
            AttachRangeIndicator(tower);

            Debug.Log($"Selected tower: {tower.gameObject.name}");
        }

        /// <summary>Deselect current tower and hide UI</summary>
        public void DeselectTower()
        {
            selectedTower = null;

            if (upgradeUI != null)
                upgradeUI.HideUpgradePanel();

            HideSelectionIndicator();
            RemoveRangeIndicator();
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

        private void AttachRangeIndicator(TowerUpgradeComponent tower)
        {
            // Remove any previous ring first
            RemoveRangeIndicator();

            var unit = tower.GetComponent<TowerUnit>();
            float radius = unit != null ? unit.EffectiveRange : 0f;
            rangeIndicator = RangeIndicator.Attach(tower.transform, radius, rangeIndicatorColor);
        }

        private void RemoveRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                Destroy(rangeIndicator.gameObject);
                rangeIndicator = null;
            }
        }
    }
}
