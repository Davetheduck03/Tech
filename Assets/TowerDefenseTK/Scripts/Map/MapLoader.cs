using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

/// <summary>
/// Loads MapData and initializes the grid at runtime.
/// Attach to the same GameObject as GridManager.
/// </summary>
public class MapLoader : MonoBehaviour
{
    [Header("Map Data")]
    public MapData mapData;

    [Header("Prefabs")]
    [SerializeField] private GameObject spawnPointPrefab;
    [SerializeField] private GameObject exitPointPrefab;
    [SerializeField] private GameObject blockedTilePrefab;

    [Header("Runtime References")]
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private GridManager gridManager;

    private void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    private void Start()
    {
        if (mapData != null)
        {
            LoadMap();
        }
    }

    /// <summary>
    /// Load the map data and setup the scene
    /// </summary>
    public void LoadMap()
    {
        if (mapData == null)
        {
            Debug.LogError("MapLoader: No MapData assigned!");
            return;
        }

        if (!mapData.Validate(out string error))
        {
            Debug.LogError($"MapLoader: Invalid map data - {error}");
            return;
        }

        // Clear previous objects
        ClearSpawnedObjects();

        // Apply grid settings
        if (gridManager != null)
        {
            gridManager.width = mapData.width;
            gridManager.height = mapData.height;
            gridManager.cellSize = mapData.cellSize;
        }

        // Initialize player resources
        InitializePlayerResources();

        // Spawn map elements after grid is generated
        StartCoroutine(SpawnMapElementsDelayed());

        Debug.Log($"MapLoader: Loaded map '{mapData.name}'");
    }

    private System.Collections.IEnumerator SpawnMapElementsDelayed()
    {
        // Wait for grid and pathfinding to initialize
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        SpawnMapElements();
        ApplyTileStates();
    }

    private void InitializePlayerResources()
    {
        // Set starting currency
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.Set(mapData.startingCurrency);
        }

        //// Set starting lives
        //if (PlayerHealthManager.Instance != null)
        //{
        //    PlayerHealthManager.Instance.Initialize(mapData.startingLives);
        //}
    }

    private void SpawnMapElements()
    {
        // Spawn spawn points
        foreach (Vector2Int spawnCoord in mapData.spawnPoints)
        {
            SpawnAtTile(spawnCoord, spawnPointPrefab, "SpawnPoint");
        }

        // Spawn exit points
        foreach (Vector2Int exitCoord in mapData.exitPoints)
        {
            SpawnAtTile(exitCoord, exitPointPrefab, "ExitPoint");
        }

        // Spawn blocked tiles (optional visual)
        foreach (TileData tile in mapData.tiles)
        {
            if (tile.type == TileType.Blocked && blockedTilePrefab != null)
            {
                SpawnAtTile(tile.coords, blockedTilePrefab, "BlockedTile");
            }
        }
    }

    private void SpawnAtTile(Vector2Int coords, GameObject prefab, string defaultName)
    {
        if (prefab == null) return;

        Vector3 worldPos = GetWorldPosition(coords);
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        obj.name = $"{defaultName} ({coords.x},{coords.y})";
        spawnedObjects.Add(obj);
    }

    private Vector3 GetWorldPosition(Vector2Int coords)
    {
        if (gridManager != null)
        {
            return gridManager.GridToWorld(coords) +
                   new Vector3(gridManager.cellSize / 2f, 0f, gridManager.cellSize / 2f);
        }

        return new Vector3(
            coords.x * mapData.cellSize + mapData.cellSize / 2f,
            0f,
            coords.y * mapData.cellSize + mapData.cellSize / 2f
        );
    }

    /// <summary>
    /// Apply tile walkability and buildability states to PathNodes
    /// </summary>
    private void ApplyTileStates()
    {
        // Find all PathNodes and apply states
        PathNode[] pathNodes = FindObjectsByType<PathNode>(FindObjectsSortMode.None);

        foreach (PathNode node in pathNodes)
        {
            TileData tile = mapData.GetTile(node.gridPosition);

            if (tile != null)
            {
                // Set walkability based on tile type
                node.isWalkable = tile.type != TileType.Blocked &&
                                  tile.type != TileType.Buildable;
            }
        }

        // Mark grid cells as occupied for non-buildable tiles
        if (gridManager != null)
        {
            foreach (TileData tile in mapData.tiles)
            {
                if (tile.type == TileType.Blocked)
                {
                    gridManager.SetCellOccupied(tile.coords, true);
                }
            }
        }

        Debug.Log("MapLoader: Applied tile states to PathNodes");
    }

    private void ClearSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
    }

    /// <summary>
    /// Get tile type at world position
    /// </summary>
    public TileType GetTileTypeAtPosition(Vector3 worldPos)
    {
        if (gridManager == null || mapData == null) return TileType.Empty;

        Vector2Int gridCoords = gridManager.WorldToGrid(worldPos);
        TileData tile = mapData.GetTile(gridCoords);

        return tile?.type ?? TileType.Empty;
    }

    /// <summary>
    /// Check if position is buildable
    /// </summary>
    public bool IsBuildableAtPosition(Vector3 worldPos)
    {
        if (gridManager == null || mapData == null) return true;

        Vector2Int gridCoords = gridManager.WorldToGrid(worldPos);
        return mapData.IsBuildable(gridCoords);
    }
}