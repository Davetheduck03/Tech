using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isWalkable = true;
    public List<PathNode> neighbors = new List<PathNode>();

    [HideInInspector] public float gCost;
    [HideInInspector] public float hCost;
    [HideInInspector] public float fCost => gCost + hCost;
    [HideInInspector] public PathNode parent;
}