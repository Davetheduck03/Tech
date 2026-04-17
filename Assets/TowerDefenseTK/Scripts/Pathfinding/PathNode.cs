using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
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

        public bool IsBuildable
        {
            get
            {
                if (_hasTower) return false;
                return _tileType == TileType.Empty ||
                       _tileType == TileType.Buildable ||
                       _tileType == TileType.Hybrid;
            }
        }

        public bool IsHybrid    => _tileType == TileType.Hybrid;
        public bool IsSpawnPoint => _tileType == TileType.Spawn;
        public bool IsExitPoint  => _tileType == TileType.Exit;

        public void NotifyNodeUpdated() => OnNodeUpdated?.Invoke(this);

        public void SetTileType(TileType type)
        {
            _tileType = type;
            UpdateWalkability();
            NotifyNodeUpdated();
        }

        private void UpdateWalkability()
        {
            if (_hasTower) { _isWalkable = false; return; }

            _isWalkable = _tileType switch
            {
                TileType.Empty     => true,
                TileType.Path      => true,
                TileType.Spawn     => true,
                TileType.Exit      => true,
                TileType.Hybrid    => true,
                TileType.Blocked   => false,
                TileType.Buildable => false,
                _ => true
            };
        }

        public void PlaceTower()
        {
            _hasTower = true;
            UpdateWalkability();
            NotifyNodeUpdated();
        }

        public void RemoveTower()
        {
            _hasTower = false;
            UpdateWalkability();
            NotifyNodeUpdated();
        }
    }
}
