using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
	/// <summary>
	/// Handles tower placement with validation for:
	/// - Grid bounds and cell occupancy
	/// - Currency affordability
	/// - Tile type (buildable vs path/blocked)
	/// - Path blocking (prevents blocking all enemy paths)
	/// - Enemy presence at placement location
	/// - Hybrid tiles (walkable + buildable, tower blocks path)
	/// </summary>
	public class TowerPlacementController : MonoBehaviour
	{
		public static TowerPlacementController Instance;

		[Header("Ground Settings")]
		[SerializeField] private float groundHeight = 0f;

		[Header("Path Validation")]
		[SerializeField] private LayerMask nodeLayer;
		[SerializeField] private float nodeDetectionRadius = 0.6f;
		[SerializeField] private bool validatePaths = true;

		[Header("Enemy Detection")]
		[SerializeField] private LayerMask enemyLayer;
		[SerializeField] private float enemyDetectionRadius = 0.5f;

		[Header("Preview Colors")]
		[SerializeField] private Color validColor = Color.green;
		[SerializeField] private Color invalidColor = Color.red;
		[SerializeField] private Color cantAffordColor = new Color(1f, 0.5f, 0f); // Orange
		[SerializeField] private Color hybridColor = new Color(0.9f, 0.7f, 0.2f); // Gold for hybrid tiles

		[Header("Settings")]
		[SerializeField] private bool exitAfterPlace = true;
		[SerializeField] private KeyCode cancelKey = KeyCode.Escape;

		// State
		private GameObject previewObject;
		private TowerSO selectedTower;
		private Plane groundPlane;
		private bool isValidPlacement;
		private bool isPlacing;
		private bool isHybridTile;

		// Cached references
		private GridManager gridManager;
		private List<Renderer> previewRenderers = new List<Renderer>();

		// Events
		public event System.Action<TowerSO> OnTowerPlaced;
		public event System.Action OnPlacementCancelled;

		#region Unity Lifecycle

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
			groundPlane = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
			gridManager = GridManager.Instance;
		}

		private void Update()
		{
			if (!isPlacing || selectedTower == null) return;

			HandlePreview();
			HandleInput();
		}

		#endregion

		#region Public API

		/// <summary>
		/// Start placing a tower
		/// </summary>
		public void SetTowerToPlace(TowerSO tower)
		{
			if (tower == null)
			{
				Debug.LogWarning("TowerPlacementController: Cannot place null tower!");
				return;
			}

			selectedTower = tower;
			isPlacing = true;
			CreatePreviewObject();

			Debug.Log($"TowerPlacementController: Started placing '{tower.UnitName}'");
		}

		/// <summary>
		/// Cancel current placement
		/// </summary>
		public void CancelPlacement()
		{
			if (previewObject != null)
			{
				Destroy(previewObject);
				previewObject = null;
			}

			previewRenderers.Clear();
			selectedTower = null;
			isPlacing = false;
			isValidPlacement = false;
			isHybridTile = false;

			OnPlacementCancelled?.Invoke();
			Debug.Log("TowerPlacementController: Placement cancelled");
		}

		/// <summary>
		/// Check if currently in placement mode
		/// </summary>
		public bool IsPlacing => isPlacing;

		/// <summary>
		/// Get the currently selected tower
		/// </summary>
		public TowerSO SelectedTower => selectedTower;

		#endregion

		#region Preview Handling

		private void CreatePreviewObject()
		{
			if (previewObject != null)
				Destroy(previewObject);

			previewRenderers.Clear();

			if (selectedTower.previewPrefab == null)
			{
				Debug.LogError($"TowerPlacementController: TowerSO '{selectedTower.UnitName}' missing previewPrefab!");
				CancelPlacement();
				return;
			}

			previewObject = Instantiate(selectedTower.previewPrefab);
			previewObject.name = $"Preview_{selectedTower.UnitName}";

			// Disable all colliders on preview
			DisableColliders(previewObject);

			// Cache renderers for color changes
			previewRenderers.AddRange(previewObject.GetComponentsInChildren<Renderer>());
		}

		private void DisableColliders(GameObject obj)
		{
			foreach (var col in obj.GetComponentsInChildren<Collider>())
			{
				col.enabled = false;
			}
		}

		private void HandlePreview()
		{
			if (previewObject == null) return;

			// Get ground position from mouse
			if (!GetGroundPosition(out Vector3 hitPoint))
			{
				previewObject.SetActive(false);
				isValidPlacement = false;
				return;
			}

			// Get grid position
			if (gridManager == null)
			{
				gridManager = GridManager.Instance;
				if (gridManager == null)
				{
					previewObject.SetActive(false);
					isValidPlacement = false;
					return;
				}
			}

			Vector2Int gridPos = gridManager.WorldToGrid(hitPoint);

			// Check if within grid bounds
			if (!gridManager.TryGetNode(gridPos, out GridNode gridNode))
			{
				previewObject.SetActive(false);
				isValidPlacement = false;
				return;
			}

			// Show preview and position it
			previewObject.SetActive(true);
			Vector3 centerPos = gridNode.worldPos + new Vector3(
				gridManager.cellSize / 2f,
				0f,
				gridManager.cellSize / 2f
			);
			previewObject.transform.position = centerPos;

			// Run validation checks
			isValidPlacement = ValidatePlacement(gridPos, centerPos, out PlacementError error);

			// Set preview color based on validation result
			SetPreviewColor(GetColorForError(error));
		}

		private Color GetColorForError(PlacementError error)
		{
			if (error == PlacementError.None && isHybridTile)
			{
				return hybridColor; // Show gold for valid hybrid placement
			}

			return error switch
			{
				PlacementError.None => validColor,
				PlacementError.CannotAfford => cantAffordColor,
				_ => invalidColor
			};
		}

		private void SetPreviewColor(Color color)
		{
			foreach (var renderer in previewRenderers)
			{
				if (renderer != null)
				{
					// Handle both standard and URP materials
					foreach (var mat in renderer.materials)
					{
						if (mat.HasProperty("_Color"))
							mat.color = color;
						if (mat.HasProperty("_BaseColor"))
							mat.SetColor("_BaseColor", color);
					}
				}
			}
		}

		#endregion

		#region Input Handling

		private void HandleInput()
		{
			// Cancel with escape or right-click
			if (Input.GetKeyDown(cancelKey) || Input.GetMouseButtonDown(1))
			{
				CancelPlacement();
				return;
			}

			// Place with left-click
			if (Input.GetMouseButtonDown(0))
			{
				TryPlaceTower();
			}
		}

		#endregion

		#region Placement Validation

		private enum PlacementError
		{
			None,
			OutOfBounds,
			CellOccupied,
			TileNotBuildable,
			EnemyPresent,
			WouldBlockPath,
			CannotAfford
		}

		private bool ValidatePlacement(Vector2Int gridPos, Vector3 worldPos, out PlacementError error)
		{
			error = PlacementError.None;
			isHybridTile = false;

			// Check 1: Cell occupancy
			if (gridManager.IsCellOccupied(gridPos))
			{
				error = PlacementError.CellOccupied;
				return false;
			}

			// Check 2: Tile type (using PathNode)
			PathNode node = null;
			if (PathNodeGenerator.Instance != null)
			{
				node = PathNodeGenerator.Instance.GetNodeAt(gridPos);
				if (node != null)
				{
					// Track if this is a hybrid tile
					isHybridTile = node.IsHybrid;

					if (!node.IsBuildable)
					{
						error = PlacementError.TileNotBuildable;
						return false;
					}
				}
			}

			// Check 3: MapLoader tile check (fallback)
			MapLoader mapLoader = FindFirstObjectByType<MapLoader>();
			if (mapLoader != null)
			{
				if (!mapLoader.CanBuildAt(worldPos))
				{
					error = PlacementError.TileNotBuildable;
					return false;
				}
				// Also check hybrid via MapLoader
				if (mapLoader.IsHybridAt(worldPos))
				{
					isHybridTile = true;
				}
			}

			// Check 4: Enemy presence
			if (IsEnemyAtPosition(worldPos))
			{
				error = PlacementError.EnemyPresent;
				return false;
			}

			// Check 5: Path blocking (critical for hybrid tiles!)
			if (validatePaths && WouldBlockAllPaths(gridPos))
			{
				error = PlacementError.WouldBlockPath;
				return false;
			}

			// Check 6: Currency (last so we show orange instead of red)
			if (CurrencyManager.Instance != null && !CurrencyManager.Instance.CanAfford(selectedTower.buildCost))
			{
				error = PlacementError.CannotAfford;
				return false;
			}

			return true;
		}

		private bool IsEnemyAtPosition(Vector3 position)
		{
			if (enemyLayer == 0) return false;

			Collider[] enemies = Physics.OverlapSphere(position, enemyDetectionRadius, enemyLayer);
			return enemies.Length > 0;
		}

		private bool WouldBlockAllPaths(Vector2Int gridPos)
		{
			// Use Astar's built-in check if available
			if (Astar.Instance != null)
			{
				return Astar.Instance.WouldBlockAllPaths(gridPos);
			}

			// Fallback: Manual check using PathNodeGenerator
			if (PathNodeGenerator.Instance == null) return false;

			PathNode nodeToBlock = PathNodeGenerator.Instance.GetNodeAt(gridPos);
			if (nodeToBlock == null) return false;

			// Temporarily mark as unwalkable
			bool originalWalkable = nodeToBlock.isWalkable;
			nodeToBlock.isWalkable = false;

			bool anyPathExists = CheckAnyPathExists();

			// Restore
			nodeToBlock.isWalkable = originalWalkable;

			return !anyPathExists;
		}

		private bool CheckAnyPathExists()
		{
			if (!NodeGetter.nodeValue.ContainsKey(NodeType.Start) ||
				!NodeGetter.nodeValue.ContainsKey(NodeType.End))
			{
				return true; // Can't validate, assume OK
			}

			foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
			{
				foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
				{
					if (startNode != null && endNode != null)
					{
						var path = Astar.Instance?.FindPath(startNode, endNode);
						if (path != null && path.Count > 0)
						{
							return true; // At least one path exists
						}
					}
				}
			}

			return false;
		}

		#endregion

		#region Tower Placement

		private void TryPlaceTower()
		{
			if (!isValidPlacement)
			{
				Debug.Log("TowerPlacementController: Invalid placement location!");
				return;
			}

			if (!GetGroundPosition(out Vector3 hitPoint))
			{
				return;
			}

			Vector2Int gridPos = gridManager.WorldToGrid(hitPoint);

			if (!gridManager.TryGetNode(gridPos, out GridNode gridNode))
			{
				return;
			}

			Vector3 centerPos = gridNode.worldPos + new Vector3(
				gridManager.cellSize / 2f,
				0f,
				gridManager.cellSize / 2f
			);

			// Final validation (in case something changed)
			if (!ValidatePlacement(gridPos, centerPos, out PlacementError error))
			{
				Debug.Log($"TowerPlacementController: Placement failed - {error}");
				return;
			}

			// Spend currency
			if (CurrencyManager.Instance != null)
			{
				if (!CurrencyManager.Instance.TrySpend(selectedTower.buildCost))
				{
					Debug.Log("TowerPlacementController: Failed to spend currency!");
					return;
				}
			}

			// Place the tower
			PlaceTowerAt(gridPos, centerPos);
		}

		private void PlaceTowerAt(Vector2Int gridPos, Vector3 worldPos)
		{
			// Instantiate tower
			GameObject towerObj = Instantiate(selectedTower.UnitPrefab, worldPos, Quaternion.identity);
			towerObj.name = $"{selectedTower.UnitName}_{gridPos.x}_{gridPos.y}";

			// Mark grid cell as occupied
			gridManager.SetCellOccupied(gridPos, true);

			// Block path node using PlaceTower() method
			// This properly handles Hybrid tiles
			if (PathNodeGenerator.Instance != null)
			{
				PathNode node = PathNodeGenerator.Instance.GetNodeAt(gridPos);
				if (node != null)
				{
					node.PlaceTower();
					Debug.Log($"TowerPlacementController: Node blocked at {gridPos} (IsHybrid: {node.IsHybrid})");
				}
			}

			// Recalculate enemy paths
			if (Astar.Instance != null)
			{
				Astar.Instance.RecalculateAndCacheAllPaths();
			}

			Debug.Log($"TowerPlacementController: Placed '{selectedTower.UnitName}' at {gridPos}");

			// Fire event
			OnTowerPlaced?.Invoke(selectedTower);

			// Exit placement mode if configured
			if (exitAfterPlace)
			{
				CancelPlacement();
			}
		}

		#endregion

		#region Utility Methods

		private bool GetGroundPosition(out Vector3 hitPoint)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (groundPlane.Raycast(ray, out float distance))
			{
				hitPoint = ray.GetPoint(distance);
				return true;
			}

			hitPoint = Vector3.zero;
			return false;
		}

		/// <summary>
		/// Sell/remove a tower and restore the grid cell
		/// For Hybrid tiles, this restores walkability
		/// </summary>
		public void RemoveTower(GameObject towerObject, Vector2Int gridPos, int refundAmount = 0)
		{
			if (towerObject == null) return;

			// Refund currency
			if (refundAmount > 0 && CurrencyManager.Instance != null)
			{
				CurrencyManager.Instance.Add(refundAmount);
			}

			// Unblock grid cell
			if (gridManager != null)
			{
				gridManager.SetCellOccupied(gridPos, false);
			}

			// Remove tower from path node (restores walkability for Hybrid)
			if (PathNodeGenerator.Instance != null)
			{
				PathNode node = PathNodeGenerator.Instance.GetNodeAt(gridPos);
				if (node != null)
				{
					node.RemoveTower();
					Debug.Log($"TowerPlacementController: Node unblocked at {gridPos} (IsWalkable: {node.isWalkable})");
				}
			}

			// Destroy tower
			Destroy(towerObject);

			// Recalculate paths
			if (Astar.Instance != null)
			{
				Astar.Instance.RecalculateAndCacheAllPaths();
			}

			Debug.Log($"TowerPlacementController: Removed tower at {gridPos}, refunded {refundAmount}");
		}

		/// <summary>
		/// Remove tower using world position
		/// </summary>
		public void RemoveTowerAt(Vector3 worldPos, int refundAmount = 0)
		{
			if (gridManager == null) return;

			Vector2Int gridPos = gridManager.WorldToGrid(worldPos);

			// Find tower at position
			Collider[] colliders = Physics.OverlapSphere(worldPos, 0.5f);
			foreach (var col in colliders)
			{
				TowerUnit tower = col.GetComponentInParent<TowerUnit>();
				if (tower != null)
				{
					RemoveTower(tower.gameObject, gridPos, refundAmount);
					return;
				}
			}
		}

		#endregion

		#region Debug

		private void OnDrawGizmos()
		{
			if (previewObject == null || !previewObject.activeInHierarchy) return;

			Vector3 pos = previewObject.transform.position;

			// Node detection sphere
			Gizmos.color = isValidPlacement ? (isHybridTile ? new Color(0.9f, 0.7f, 0.2f) : Color.green) : Color.red;
			Gizmos.DrawWireSphere(pos + Vector3.up * 0.75f, nodeDetectionRadius);

			// Enemy detection sphere
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(pos, enemyDetectionRadius);
		}

		#endregion
	}
}