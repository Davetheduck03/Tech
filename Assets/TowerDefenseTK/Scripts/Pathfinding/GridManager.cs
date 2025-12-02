using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
    }

    public Dictionary<Vector2Int, GridNode> GetAllNodes()
    {
        return grid;
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coords = new Vector2Int(x, y);
                Vector3 world = GridToWorld(coords);

                grid[coords] = new GridNode(coords, world);
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int coords)
    {
        return new Vector3(coords.x * cellSize, 0f, coords.y * cellSize);
    }

    public bool TryGetNode(Vector2Int coords, out GridNode node)
    {
        return grid.TryGetValue(coords, out node);
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int x = Mathf.FloorToInt(world.x / cellSize);
        int y = Mathf.FloorToInt(world.z / cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsCellOccupied(Vector2Int coords)
    {
        return grid.ContainsKey(coords) && grid[coords].occupied;
    }

    public void SetCellOccupied(Vector2Int coords, bool value)
    {
        if (grid.ContainsKey(coords))
            grid[coords].occupied = value;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize);
                Vector3 size = new Vector3(cellSize, 0f, cellSize);

                Gizmos.DrawWireCube(worldPos, size);
            }
        }
    }

}
