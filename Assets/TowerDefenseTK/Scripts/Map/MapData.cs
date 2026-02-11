using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
	[CreateAssetMenu(fileName = "New Map", menuName = "TD Toolkit/Map Data")]
	public class MapData : ScriptableObject
	{
		[Header("Grid Settings")]
		public int width = 10;
		public int height = 10;
		public float cellSize = 1f;

		[Header("Tile Data")]
		public List<TileData> tiles = new List<TileData>();

		[Header("Spawn Points")]
		public List<Vector2Int> spawnPoints = new List<Vector2Int>();

		[Header("Exit Points")]
		public List<Vector2Int> exitPoints = new List<Vector2Int>();

		[Header("Player Settings")]
		public int startingCurrency = 500;
		public int startingLives = 20;

		/// <summary>
		/// Get tile at coordinates, returns null if not found
		/// </summary>
		public TileData GetTile(Vector2Int coords)
		{
			return tiles.Find(t => t.coords == coords);
		}

		/// <summary>
		/// Set tile data at coordinates
		/// </summary>
		public void SetTile(Vector2Int coords, TileType type)
		{
			TileData existing = GetTile(coords);

			if (existing != null)
			{
				existing.type = type;
			}
			else
			{
				tiles.Add(new TileData(coords, type));
			}

			// Handle spawn/exit point lists
			UpdateSpecialPoints(coords, type);
		}

		private void UpdateSpecialPoints(Vector2Int coords, TileType type)
		{
			// Remove from special lists first
			spawnPoints.Remove(coords);
			exitPoints.Remove(coords);

			// Add to appropriate list
			if (type == TileType.Spawn && !spawnPoints.Contains(coords))
			{
				spawnPoints.Add(coords);
			}
			else if (type == TileType.Exit && !exitPoints.Contains(coords))
			{
				exitPoints.Add(coords);
			}
		}

		/// <summary>
		/// Check if a tile is walkable (enemies can travel)
		/// </summary>
		public bool IsWalkable(Vector2Int coords)
		{
			TileData tile = GetTile(coords);
			if (tile == null) return true; // Default walkable

			// Hybrid is walkable (until tower placed - handled at runtime)
			return tile.type == TileType.Empty ||
				   tile.type == TileType.Path ||
				   tile.type == TileType.Spawn ||
				   tile.type == TileType.Exit ||
				   tile.type == TileType.Hybrid;
		}

		/// <summary>
		/// Check if a tile is buildable (towers can be placed)
		/// </summary>
		public bool IsBuildable(Vector2Int coords)
		{
			TileData tile = GetTile(coords);
			if (tile == null) return true; // Default buildable

			return tile.type == TileType.Buildable ||
				   tile.type == TileType.Empty ||
				   tile.type == TileType.Hybrid;
		}

		/// <summary>
		/// Check if tile is hybrid (walkable AND buildable)
		/// </summary>
		public bool IsHybrid(Vector2Int coords)
		{
			TileData tile = GetTile(coords);
			return tile != null && tile.type == TileType.Hybrid;
		}

		/// <summary>
		/// Clear all tile data
		/// </summary>
		public void ClearAll()
		{
			tiles.Clear();
			spawnPoints.Clear();
			exitPoints.Clear();
		}

		/// <summary>
		/// Initialize with default empty grid
		/// </summary>
		public void InitializeEmpty()
		{
			ClearAll();

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					tiles.Add(new TileData(new Vector2Int(x, y), TileType.Empty));
				}
			}
		}

		/// <summary>
		/// Validate the map has required elements
		/// </summary>
		public bool Validate(out string error)
		{
			if (spawnPoints.Count == 0)
			{
				error = "Map needs at least one spawn point!";
				return false;
			}

			if (exitPoints.Count == 0)
			{
				error = "Map needs at least one exit point!";
				return false;
			}

			error = string.Empty;
			return true;
		}
	}

	[Serializable]
	public class TileData
	{
		public Vector2Int coords;
		public TileType type;

		public TileData(Vector2Int coords, TileType type)
		{
			this.coords = coords;
			this.type = type;
		}
	}

	public enum TileType
	{
		Empty,      // Walkable, buildable
		Path,       // Walkable, NOT buildable (enemy path)
		Blocked,    // NOT walkable, NOT buildable (walls/obstacles)
		Buildable,  // NOT walkable, buildable (tower spots only)
		Spawn,      // Enemy spawn point (walkable, NOT buildable)
		Exit,       // Enemy exit point (walkable, NOT buildable)
		Hybrid      // Walkable AND buildable - tower placement blocks the path
	}
}