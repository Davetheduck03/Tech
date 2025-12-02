using UnityEngine;
using System.Collections.Generic;
using TowerDefenseTK;
using System.Linq;
using Unity.VisualScripting;

public class CustomPathHandler : MonoBehaviour
{
    public List<PathNode> pathNodes;
    private PathNode startNode;
    private PathNode endNode;
    public Transform endGoal;
    [SerializeField] private LayerMask nodeLayer;

    private void OnEnable()
    {
        PathNodeGenerator.OnGridGenerated += Init;
    }

    private void OnDisable()
    {
        PathNodeGenerator.OnGridGenerated -= Init;
    }

    private void Init()
    {
        var nodeGetterComponent = this.gameObject.GetComponent<NodeGetter>();
        startNode = NodeGetter.GetNodeBelow(transform.position + Vector3.up * 1f, nodeLayer);
        endNode = NodeGetter.GetNodeBelow(endGoal.position + Vector3.up * 1f, nodeLayer);
        pathNodes.Add(endNode);
        pathNodes.Insert(0, startNode);
        Astar.Instance.customPathCache.Add((startNode, endNode), pathNodes);
    }

    private void OnDrawGizmos()
    {
        if (pathNodes == null || pathNodes.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        foreach (var node in pathNodes)
        {
            if (node != null)
                Gizmos.DrawSphere(node.transform.position, 0.2f);
        }

        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            if (pathNodes[i] != null && pathNodes[i + 1] != null)
            {
                Gizmos.DrawLine(pathNodes[i].transform.position, pathNodes[i + 1].transform.position);
            }
        }
    }

}
