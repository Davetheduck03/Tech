using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{

    public class PathNodeGenerator : MonoBehaviour
    {
        public static event Action OnGridGenerated;

        public static PathNodeGenerator Instance;

        [Header("Node Settings")]
        public GameObject nodePrefab;

        private Dictionary<Vector2Int, PathNode> pathNodes = new Dictionary<Vector2Int, PathNode>();

        private void Start()
        {
            PathNodeGenerator.Instance = this;
            GenerateNodes();
            LinkNeighbors();
            StartCoroutine(RandomBS());
        }

        private void GenerateNodes()
        {
            GridManager gm = GridManager.Instance;
            if (gm == null)
            {
                Debug.LogError("GridManager not found in scene.");
                return;
            }

            foreach (var kvp in gm.GetAllNodes())
            {
                Vector2Int coords = kvp.Key;
                GridNode gridNode = kvp.Value;

                if (pathNodes.ContainsKey(coords))
                {
                    PathNode existing = pathNodes[coords];
                    existing.gridPosition = coords;
                    existing.isWalkable = true;
                    continue;
                }

                Vector3 spawnPos = gridNode.worldPos + new Vector3(
                    gm.cellSize / 2f,
                    0.75f,
                    gm.cellSize / 2f
                );


                GameObject nodeObj = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);
                nodeObj.name = $"PathNode ({coords.x},{coords.y})";

                PathNode pathNode = nodeObj.GetComponent<PathNode>();
                pathNode.gridPosition = coords;
                pathNode.isWalkable = true;

                pathNodes.Add(coords, pathNode);
                Astar.Instance.allNodes.Add(pathNode);
            }
        }

        private IEnumerator RandomBS()
        {
            yield return new WaitForEndOfFrame();
            OnGridGenerated?.Invoke();
        }

        private void LinkNeighbors()
        {
            foreach (var kvp in pathNodes)
            {
                Vector2Int coord = kvp.Key;
                PathNode node = kvp.Value;

                node.neighbors.Clear();

                TryAddNeighbor(node, new Vector2Int(coord.x - 1, coord.y));
                TryAddNeighbor(node, new Vector2Int(coord.x + 1, coord.y));
                TryAddNeighbor(node, new Vector2Int(coord.x, coord.y - 1));
                TryAddNeighbor(node, new Vector2Int(coord.x, coord.y + 1));
            }
        }

        private void TryAddNeighbor(PathNode node, Vector2Int coords)
        {
            if (pathNodes.TryGetValue(coords, out PathNode neighbor))
            {
                node.neighbors.Add(neighbor);
            }
        }
    }
}
