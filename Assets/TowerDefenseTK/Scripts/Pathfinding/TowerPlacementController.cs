using System.Collections.Generic;
using TowerDefenseTK;
using UnityEngine;

public class TowerPlacementController : MonoBehaviour
{
    public static TowerPlacementController Instance;

    [Header("Ground Settings")]
    public float groundHeight = 0f;

    [Header("Path Validation")]
    [SerializeField] private LayerMask nodeLayer;
    [SerializeField] private float nodeDetectionRadius = 0.6f;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float enemyDetectionRadius = 0.5f;

    private GameObject previewObject;
    private TowerSO selectedTower;
    private Plane groundPlane;

    private bool isValidPlacement;
    private List<PathNode> temporarilyBlockedNodes = new List<PathNode>();

    // Public property to check if in placement mode
    public bool IsPlacingTower => selectedTower != null;

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
    }

    private void Update()
    {
        if (selectedTower == null) return;

        HandlePreview();
        HandlePlacement();
    }

    public void SetTowerToPlace(TowerSO tower)
    {
        selectedTower = tower;
        CreatePreviewObject();
    }

    public void CancelPlacement()
    {
        selectedTower = null;
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        ClearTemporaryBlocks();
    }

    private void CreatePreviewObject()
    {
        if (previewObject != null)
            Destroy(previewObject);

        if (selectedTower.previewPrefab == null)
        {
            Debug.LogError("TowerSO missing previewPrefab!");
            return;
        }

        previewObject = Instantiate(selectedTower.previewPrefab);
        DisableColliders(previewObject);
    }

    private void DisableColliders(GameObject obj)
    {
        foreach (var c in obj.GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    private void HandlePreview()
    {
        if (previewObject == null) return;

        if (!GetGroundPosition(out Vector3 hit))
        {
            previewObject.SetActive(false);
            isValidPlacement = false;
            return;
        }

        GridManager gm = GridManager.Instance;
        Vector2Int gridPos = gm.WorldToGrid(hit);

        if (!gm.TryGetNode(gridPos, out GridNode node))
        {
            previewObject.SetActive(false);
            isValidPlacement = false;
            return;
        }

        previewObject.SetActive(true);
        Vector3 centerPos = node.worldPos + new Vector3(gm.cellSize / 2f, 0f, gm.cellSize / 2f);
        previewObject.transform.position = centerPos;

        MapLoader mapLoader = FindFirstObjectByType<MapLoader>();
        if (mapLoader != null)
        {
            TileType tileType = mapLoader.GetTileTypeAtPosition(centerPos);

            // Can only build on Empty or Buildable tiles
            if (tileType != TileType.Empty && tileType != TileType.Buildable)
            {
                SetPreviewColor(Color.red);
                isValidPlacement = false;
                return;
            }
        }

        // Check if cell is occupied
        if (node.occupied)
        {
            SetPreviewColor(Color.red);
            isValidPlacement = false;
            return;
        }

        // Check if enemy is at this position
        if (IsEnemyAtPosition(centerPos))
        {
            SetPreviewColor(Color.red);
            isValidPlacement = false;
            return;
        }

        // Check if can afford
        if (!CurrencyManager.Instance.CanAfford(selectedTower.buildCost))
        {
            SetPreviewColor(Color.red);
            isValidPlacement = false;
            return;
        }

        // Check if placement would block all paths
        isValidPlacement = ValidatePathsAtPosition(centerPos);
        SetPreviewColor(isValidPlacement ? Color.green : Color.red);
    }

    private bool IsEnemyAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, enemyDetectionRadius, enemyLayer);
        return colliders.Length > 0;
    }

    private bool ValidatePathsAtPosition(Vector3 position)
    {
        ClearTemporaryBlocks();

        Vector3 checkPos = new Vector3(position.x, position.y + 0.75f, position.z);
        Collider[] colliders = Physics.OverlapSphere(checkPos, nodeDetectionRadius, nodeLayer);

        if (colliders.Length == 0)
        {
            return true;
        }

        foreach (var col in colliders)
        {
            PathNode node = col.GetComponent<PathNode>();
            if (node != null && node.isWalkable)
            {
                node.isWalkable = false;
                temporarilyBlockedNodes.Add(node);
            }
        }

        if (temporarilyBlockedNodes.Count == 0)
        {
            return true;
        }

        bool allPathsValid = CheckAllPathsValid();

        ClearTemporaryBlocks();

        return allPathsValid;
    }

    private bool CheckAllPathsValid()
    {
        if (Astar.Instance == null)
        {
            Debug.LogWarning("Astar.Instance is null!");
            return true;
        }

        if (!NodeGetter.nodeValue.ContainsKey(NodeType.Start) ||
            NodeGetter.nodeValue[NodeType.Start].Count == 0)
        {
            Debug.LogWarning("No start nodes found!");
            return true;
        }

        if (!NodeGetter.nodeValue.ContainsKey(NodeType.End) ||
            NodeGetter.nodeValue[NodeType.End].Count == 0)
        {
            Debug.LogWarning("No end nodes found!");
            return true;
        }

        foreach (var startNode in NodeGetter.nodeValue[NodeType.Start])
        {
            foreach (var endNode in NodeGetter.nodeValue[NodeType.End])
            {
                if (startNode == null || endNode == null) continue;

                List<PathNode> testPath = Astar.Instance.FindPath(startNode, endNode);

                if (testPath == null || testPath.Count == 0)
                {
                    Debug.Log($"Placement would block path: {startNode.name} → {endNode.name}");
                    return false;
                }
            }
        }

        return true;
    }

    private void ClearTemporaryBlocks()
    {
        foreach (var node in temporarilyBlockedNodes)
        {
            if (node != null)
            {
                node.isWalkable = true;
            }
        }
        temporarilyBlockedNodes.Clear();
    }

    private void SetPreviewColor(Color c)
    {
        foreach (var r in previewObject.GetComponentsInChildren<MeshRenderer>())
        {
            r.material.color = c;
        }
    }

    private void HandlePlacement()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (!isValidPlacement)
        {
            Debug.Log("Cannot place here!");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(selectedTower.buildCost))
        {
            Debug.Log("Not enough currency!");
            return;
        }

        if (!GetGroundPosition(out Vector3 hit)) return;

        GridManager gm = GridManager.Instance;
        Vector2Int gridPos = gm.WorldToGrid(hit);

        if (!gm.TryGetNode(gridPos, out GridNode node)) return;

        if (node.occupied)
        {
            Debug.Log("Cell is occupied!");
            return;
        }

        Vector3 centerPos = node.worldPos + new Vector3(gm.cellSize / 2f, 0f, gm.cellSize / 2f);

        if (IsEnemyAtPosition(centerPos))
        {
            Debug.Log("Cannot place on enemy!");
            return;
        }

        // Subtract currency and place tower
        CurrencyManager.Instance.TrySpend(selectedTower.buildCost);
        Instantiate(selectedTower.UnitPrefab, centerPos, Quaternion.identity);
        gm.SetCellOccupied(gridPos, true);

        // Exit placement state after successful placement
        Debug.Log($"Tower '{selectedTower.UnitName}' placed successfully! Exiting placement mode.");
        CancelPlacement();
    }

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

    private void OnDrawGizmos()
    {
        if (previewObject == null || !previewObject.activeInHierarchy) return;

        Vector3 pos = previewObject.transform.position;

        // Node detection sphere
        Gizmos.color = isValidPlacement ? Color.green : Color.red;
        Gizmos.DrawWireSphere(pos + Vector3.up * 0.75f, nodeDetectionRadius);

        // Enemy detection sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, enemyDetectionRadius);
    }
}