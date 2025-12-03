using UnityEngine;

public class TowerPlacementController : MonoBehaviour
{
    // Singleton
    public static TowerPlacementController Instance;

    [Header("Raycast Settings")]
    public LayerMask groundMask;

    private GameObject previewObject;
    private TowerSO selectedTower;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        if (!RaycastToGround(out Vector3 hit))
        {
            previewObject.SetActive(false);
            return;
        }

        GridManager gm = GridManager.Instance;

        Vector2Int gridPos = gm.WorldToGrid(hit);



        if (!gm.TryGetNode(gridPos, out GridNode node))
        {
            previewObject.SetActive(false);
            return;
        }

        previewObject.SetActive(true);

        Vector3 centerPos = node.worldPos + new Vector3(gm.cellSize / 2f, 0f, gm.cellSize / 2f);

        previewObject.transform.position = centerPos;

        if (node.occupied)
            SetPreviewColor(Color.red);
        else
            SetPreviewColor(Color.green);
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
        if (!RaycastToGround(out Vector3 hit)) return;

        GridManager gm = GridManager.Instance;
        Vector2Int gridPos = gm.WorldToGrid(hit);



        if (!gm.TryGetNode(gridPos, out GridNode node)) return;

        if (node.occupied)
        {
            Debug.Log("Cell is occupied!");
            return;
        }

        Vector3 centerPos = node.worldPos + new Vector3(gm.cellSize / 2f, 0f, gm.cellSize / 2f);

        Instantiate(selectedTower.UnitPrefab, centerPos, Quaternion.identity);

        gm.SetCellOccupied(gridPos, true);
    }

    private bool RaycastToGround(out Vector3 hitPoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 300f, groundMask))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }
}