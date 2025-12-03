using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Debug Settings")]
    public bool drawGridInGame = true;
    public Material lineMaterial;

    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();
    private List<LineRenderer> gridLines = new List<LineRenderer>();

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
        if (drawGridInGame)
            DrawGridLines();
    }

    public Dictionary<Vector2Int, GridNode> GetAllNodes() => grid;

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

    public bool TryGetNode(Vector2Int coords, out GridNode node) => grid.TryGetValue(coords, out node);

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int x = Mathf.FloorToInt(world.x / cellSize);
        int y = Mathf.FloorToInt(world.z / cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsCellOccupied(Vector2Int coords) => grid.ContainsKey(coords) && grid[coords].occupied;

    public void SetCellOccupied(Vector2Int coords, bool value)
    {
        if (grid.ContainsKey(coords))
            grid[coords].occupied = value;
    }

    private void DrawGridLines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 bottomLeft = GridToWorld(new Vector2Int(x, y));
                Vector3 bottomRight = bottomLeft + new Vector3(cellSize, 0, 0);
                Vector3 topRight = bottomLeft + new Vector3(cellSize, 0, cellSize);
                Vector3 topLeft = bottomLeft + new Vector3(0, 0, cellSize);

                DrawLine(bottomLeft, bottomRight);
                DrawLine(bottomRight, topRight);
                DrawLine(topRight, topLeft);
                DrawLine(topLeft, bottomLeft);
            }
        }
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start + Vector3.up * 0.01f); // slight offset so it doesn't z-fight with floor
        lr.SetPosition(1, end + Vector3.up * 0.01f);
        lr.loop = false;

        gridLines.Add(lr);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 bottomLeft = new Vector3(x * cellSize, 0.01f, y * cellSize);
                Vector3 bottomRight = bottomLeft + new Vector3(cellSize, 0, 0);
                Vector3 topRight = bottomLeft + new Vector3(cellSize, 0, cellSize);
                Vector3 topLeft = bottomLeft + new Vector3(0, 0, cellSize);

                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }
        }
    }

}
