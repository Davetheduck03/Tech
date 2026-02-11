using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
	/// <summary>
	/// Loads MapData and coordinates between GridManager and PathNodeGenerator
	/// </summary>
	public class MapLoader : MonoBehaviour
	{
		[Header("Map Data")]
		public MapData mapData;

		[Header("Prefabs")]
		[SerializeField] private GameObject spawnPointVisualPrefab;
		[SerializeField] private GameObject exitPointVisualPrefab;
		[SerializeField] private GameObject blockedTileVisualPrefab;
		[SerializeField] private GameObject hybridTileVisualPrefab;

		[Header("Settings")]
		[SerializeField] private bool spawnVisuals = true;

		private List<GameObject> spawnedVisuals = new List<GameObject>();

		private void Awake()
		{
			ApplyGridSettings();
		}

		/// <summary>
		/// Apply map settings to GridManager before it generates
		/// </summary>
		private void ApplyGridSettings()
		{
			if (mapData == null) return;

			GridManager gm = GridManager.Instance;
			if (gm == null)
			{
				gm = FindFirstObjectByType<GridManager>();
			}

			if (gm != null)
			{
				gm.width = mapData.width;
				gm.height = mapData.height;
				gm.cellSize = mapData.cellSize;

				Debug.Log($"MapLoader: Applied grid settings - {mapData.width}x{mapData.height}, cell size {mapData.cellSize}");
			}
		}

		private void Start()
		{
			if (mapData != null)
			{
				StartCoroutine(InitializeMap());
			}
		}

		private IEnumerator InitializeMap()
		{
			// Wait for grid and pathfinding to initialize
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();

			InitializePlayerResources();

			if (spawnVisuals)
			{
				SpawnMapVisuals();
			}
		}

		private void InitializePlayerResources()
		{
			if (CurrencyManager.Instance != null)
			{
				CurrencyManager.Instance.Set(mapData.startingCurrency);
			}

			//if (PlayerHealthManager.Instance != null)
			//{
			//    PlayerHealthManager.Instance.Initialize(mapData.startingLives);
			//}

			Debug.Log($"MapLoader: Initialized with {mapData.startingCurrency} currency, {mapData.startingLives} lives");
		}

		private void SpawnMapVisuals()
		{
			ClearSpawnedVisuals();

			// Spawn visual markers for spawn points
			foreach (Vector2Int coords in mapData.spawnPoints)
			{
				SpawnVisualAt(coords, spawnPointVisualPrefab, "SpawnPoint");
			}

			// Spawn visual markers for exit points
			foreach (Vector2Int coords in mapData.exitPoints)
			{
				SpawnVisualAt(coords, exitPointVisualPrefab, "ExitPoint");
			}

			// Spawn blocked and hybrid tile visuals
			foreach (TileData tile in mapData.tiles)
			{
				if (tile.type == TileType.Blocked && blockedTileVisualPrefab != null)
				{
					SpawnVisualAt(tile.coords, blockedTileVisualPrefab, "BlockedTile");
				}
				else if (tile.type == TileType.Hybrid && hybridTileVisualPrefab != null)
				{
					SpawnVisualAt(tile.coords, hybridTileVisualPrefab, "HybridTile");
				}
			}
		}

		private void SpawnVisualAt(Vector2Int coords, GameObject prefab, string namePrefix)
		{
			if (prefab == null) return;

			Vector3 worldPos = GetWorldPosition(coords);
			GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
			obj.name = $"{namePrefix} ({coords.x},{coords.y})";
			spawnedVisuals.Add(obj);
		}

		private Vector3 GetWorldPosition(Vector2Int coords)
		{
			if (GridManager.Instance != null)
			{
				return GridManager.Instance.GridToWorld(coords) +
					   new Vector3(GridManager.Instance.cellSize / 2f, 0f, GridManager.Instance.cellSize / 2f);
			}

			return new Vector3(
				coords.x * mapData.cellSize + mapData.cellSize / 2f,
				0f,
				coords.y * mapData.cellSize + mapData.cellSize / 2f
			);
		}

		private void ClearSpawnedVisuals()
		{
			foreach (GameObject obj in spawnedVisuals)
			{
				if (obj != null)
				{
					Destroy(obj);
				}
			}
			spawnedVisuals.Clear();
		}

		/// <summary>
		/// Get tile type at world position
		/// </summary>
		public TileType GetTileTypeAt(Vector3 worldPos)
		{
			if (mapData == null) return TileType.Empty;

			Vector2Int coords = GridManager.Instance != null
				? GridManager.Instance.WorldToGrid(worldPos)
				: new Vector2Int(
					Mathf.FloorToInt(worldPos.x / mapData.cellSize),
					Mathf.FloorToInt(worldPos.z / mapData.cellSize)
				);

			TileData tile = mapData.GetTile(coords);
			return tile?.type ?? TileType.Empty;
		}

		/// <summary>
		/// Check if position allows building (includes Hybrid tiles)
		/// </summary>
		public bool CanBuildAt(Vector3 worldPos)
		{
			TileType type = GetTileTypeAt(worldPos);
			return type == TileType.Empty || type == TileType.Buildable || type == TileType.Hybrid;
		}

		/// <summary>
		/// Check if tile at position is hybrid
		/// </summary>
		public bool IsHybridAt(Vector3 worldPos)
		{
			return GetTileTypeAt(worldPos) == TileType.Hybrid;
		}
	}
}