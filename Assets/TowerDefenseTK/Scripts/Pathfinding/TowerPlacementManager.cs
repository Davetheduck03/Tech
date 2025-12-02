using UnityEngine;

public class TowerPlacementManager : MonoBehaviour
{
    [Header("References")]
    public GameObject towerPrefab;
    public GameObject previewPrefab;
    public LayerMask groundMask;

    private GameObject previewObject;

    void Start()
    {
        // Create preview object and remove collider
        previewObject = Instantiate(previewPrefab);
        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    void Update()
    {
        HandlePreview();
        HandlePlacement();
    }

    void HandlePreview()
    {
        if (!RaycastToGround(out Vector3 hit))
        {
            previewObject.SetActive(false);
            return;
        }

        GridManager gm = GridManager.Instance;

        previewObject.SetActive(true);

        Vector2Int gridPos = gm.WorldToGrid(hit);

        if (!gm.TryGetNode(gridPos, out GridNode node))
        {
            previewObject.SetActive(false);
            return;
        }

        previewObject.transform.position = node.worldPos;

        SetPreviewColor(node.occupied ? Color.red : Color.green);
    }

    void HandlePlacement()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!RaycastToGround(out Vector3 hit)) return;

        GridManager gm = GridManager.Instance;
        Vector2Int gridPos = gm.WorldToGrid(hit);

        if (!gm.TryGetNode(gridPos, out GridNode node)) return;
        if (node.occupied) return;

        Instantiate(towerPrefab, node.worldPos, Quaternion.identity);

        gm.SetCellOccupied(gridPos, true);
    }

    void SetPreviewColor(Color c)
    {
        foreach (var mesh in previewObject.GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material.color = c;
        }
    }

    bool RaycastToGround(out Vector3 hitPoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }
}
