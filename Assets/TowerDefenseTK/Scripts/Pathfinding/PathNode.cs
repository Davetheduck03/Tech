using System.Collections.Generic;
using UnityEngine;
using TowerDefenseTK;

public class PathNode : MonoBehaviour
{
    public static event System.Action<PathNode> OnNodeUpdated;

    public Vector2Int gridPosition;
    public List<PathNode> neighbors = new List<PathNode>();

    [Header("Tile Properties")]
    [SerializeField] private TileType _tileType = TileType.Empty;

    [HideInInspector] public float gCost;
    [HideInInspector] public float hCost;
    [HideInInspector] public float fCost => gCost + hCost;
    [HideInInspector] public PathNode parent;

    public TileType TileType
    {
        get => _tileType;
        set
        {
            if (_tileType != value)
            {
                _tileType = value;
                NotifyNodeUpdated();
            }
        }
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

    /// <summary>
    /// Check if this tile allows tower building
    /// </summary>
    public bool IsBuildable
    {
        get
        {
            // Can build on Empty or Buildable tiles, not on paths, spawn, exit, or blocked
            return _tileType == TileType.Empty || _tileType == TileType.Buildable;
        }
    }

    /// <summary>
    /// Check if this is a spawn point
    /// </summary>
    public bool IsSpawnPoint => _tileType == TileType.Spawn;

    /// <summary>
    /// Check if this is an exit point
    /// </summary>
    public bool IsExitPoint => _tileType == TileType.Exit;

    public void NotifyNodeUpdated()
    {
        OnNodeUpdated?.Invoke(this);
    }

    /// <summary>
    /// Apply tile type and set walkability accordingly
    /// </summary>
    public void SetTileType(TileType type)
    {
        _tileType = type;

        // Determine walkability based on tile type
        _isWalkable = type switch
        {
            TileType.Empty => true,
            TileType.Path => true,
            TileType.Spawn => true,
            TileType.Exit => true,
            TileType.Blocked => false,
            TileType.Buildable => false, // Buildable spots block enemy paths
            _ => true
        };

        NotifyNodeUpdated();
    }
}