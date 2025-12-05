using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public static event System.Action<PathNode> OnNodeUpdated;

    public Vector2Int gridPosition;
    public List<PathNode> neighbors = new List<PathNode>();

    [HideInInspector] public float gCost;
    [HideInInspector] public float hCost;
    [HideInInspector] public float fCost => gCost + hCost;
    [HideInInspector] public PathNode parent;

    public void NotifyNodeUpdated()
    {
        OnNodeUpdated?.Invoke(this);
    }

    private bool _isWalkable = true;
    public bool isWalkable
    {
        get => _isWalkable;
        set
        {
            if (_isWalkable != value)
            {
                _isWalkable = value;
                NotifyNodeUpdated();
            }
        }

    }
}