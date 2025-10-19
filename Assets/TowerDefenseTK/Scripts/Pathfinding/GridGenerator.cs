using UnityEngine;

namespace TowerDefenseTK
{

    public class GridGenerator : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int width = 10;
        public int height = 10;
        public float cellSize = 1f;

        [Header("Node Settings")]
        public GameObject nodePrefab;

        private PathNode[,] grid;

        private void Start()
        {
            GenerateGrid();
            LinkNeighbors();
        }

        private void GenerateGrid()
        {
            grid = new PathNode[width, height];

            Vector3 origin = transform.position;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 📍 Center of each cell
                    Vector3 worldPos = origin + new Vector3(x * cellSize + cellSize / 2f, 0.75f, y * cellSize + cellSize / 2f);

                    // Spawn node in the center of the cell
                    GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity, transform);
                    nodeObj.name = $"Node ({x},{y})";

                    PathNode node = nodeObj.GetComponent<PathNode>();
                    node.gridPosition = new Vector2Int(x, y);
                    node.isWalkable = true;

                    grid[x, y] = node;

                    // Register node to the pathfinding system
                    Astar.Instance.allNodes.Add(node);
                }
            }
        }

        private void LinkNeighbors()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    PathNode node = grid[x, y];
                    node.neighbors.Clear();

                    // 🔹 4-directional neighbors (N/E/S/W)
                    if (x > 0) node.neighbors.Add(grid[x - 1, y]);
                    if (x < width - 1) node.neighbors.Add(grid[x + 1, y]);
                    if (y > 0) node.neighbors.Add(grid[x, y - 1]);
                    if (y < height - 1) node.neighbors.Add(grid[x, y + 1]);
                }
            }
        }

        public PathNode GetNodeAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return null;

            return grid[x, y];
        }

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position;

            // Draw grid lines
            Gizmos.color = Color.yellow;
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0, 0);
                Vector3 end = origin + new Vector3(x * cellSize, 0, height * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= height; y++)
            {
                Vector3 start = origin + new Vector3(0, 0, y * cellSize);
                Vector3 end = origin + new Vector3(width * cellSize, 0, y * cellSize);
                Gizmos.DrawLine(start, end);
            }

            // 🟥 Draw blocked cells
            if (grid != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        PathNode node = grid[x, y];
                        if (node != null && !node.isWalkable)
                        {
                            Gizmos.color = new Color(1, 0, 0, 0.5f);
                            Gizmos.DrawCube(node.transform.position + Vector3.up * 0.01f, Vector3.one * (cellSize * 0.9f));
                        }
                    }
                }
            }
        }
    }
}
