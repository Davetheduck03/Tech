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
	[SerializeField] private bool _hasTower = false;

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
				UpdateWalkability();
				NotifyNodeUpdated();
			}
		}
	}

	/// <summary>
	/// Whether this node currently has a tower on it
	/// </summary>
	public bool HasTower
	{
		get => _hasTower;
		private set => _hasTower = value;
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
			// Can't build if already has a tower
			if (_hasTower) return false;

			// Can build on Empty, Buildable, or Hybrid tiles
			return _tileType == TileType.Empty ||
				   _tileType == TileType.Buildable ||
				   _tileType == TileType.Hybrid;
		}
	}

	/// <summary>
	/// Check if this is a hybrid tile
	/// </summary>
	public bool IsHybrid => _tileType == TileType.Hybrid;

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
		UpdateWalkability();
		NotifyNodeUpdated();
	}

	/// <summary>
	/// Update walkability based on tile type and tower presence
	/// </summary>
	private void UpdateWalkability()
	{
		// If there's a tower, node is blocked
		if (_hasTower)
		{
			_isWalkable = false;
			return;
		}

		// Determine walkability based on tile type
		_isWalkable = _tileType switch
		{
			TileType.Empty => true,
			TileType.Path => true,
			TileType.Spawn => true,
			TileType.Exit => true,
			TileType.Hybrid => true,      // Walkable when no tower
			TileType.Blocked => false,
			TileType.Buildable => false,  // Buildable-only spots block paths
			_ => true
		};
	}

	/// <summary>
	/// Place a tower on this node (blocks walkability)
	/// </summary>
	public void PlaceTower()
	{
		_hasTower = true;
		UpdateWalkability();
		NotifyNodeUpdated();
	}

	/// <summary>
	/// Remove tower from this node (restores walkability for Hybrid)
	/// </summary>
	public void RemoveTower()
	{
		_hasTower = false;
		UpdateWalkability();
		NotifyNodeUpdated();
	}
}