using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Blocks PathNodes and triggers Astar to recalculate all paths.
    /// Astar handles reassigning paths to all active units.
    /// </summary>
    public class PathObstacle : MonoBehaviour
    {
        [SerializeField] private LayerMask nodeLayer;
        [SerializeField] private float detectionRadius = 0.6f;
        [SerializeField] private bool blockOnStart = true;

        private List<PathNode> blockedNodes = new List<PathNode>();
        private bool isBlocking = false;

        private void Start()
        {
            if (blockOnStart)
            {
                // Small delay to ensure everything is initialized
                Invoke(nameof(BlockPath), 0.1f);
            }
        }

        /// <summary>
        /// Block nodes and trigger path recalculation
        /// </summary>
        public void BlockPath()
        {
            if (isBlocking)
            {
                Debug.LogWarning($"{gameObject.name}: Already blocking path!");
                return;
            }

            Debug.Log($"{gameObject.name}: Blocking path...");

            // Find all nodes within radius
            Collider[] colliders = Physics.OverlapSphere(new Vector3(transform.position.x, transform.position.y + 0.75f, transform.position.z), detectionRadius, nodeLayer);

            foreach (var col in colliders)
            {
                PathNode node = col.GetComponent<PathNode>();
                if (node != null && !blockedNodes.Contains(node))
                {
                    node.isWalkable = false;
                    blockedNodes.Add(node);
                    Debug.Log($"{gameObject.name}: Blocked node {node.name}");
                }
            }

            if (blockedNodes.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: No nodes found to block!");
                return;
            }

            isBlocking = true;

            // Tell Astar to recalculate all paths and reassign to units
            if (Astar.Instance != null)
            {
                Astar.Instance.RecalculateAndCacheAllPaths();
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Astar instance not found!");
            }
        }

        /// <summary>
        /// Unblock nodes and trigger path recalculation
        /// </summary>
        public void UnblockPath()
        {
            if (!isBlocking)
            {
                Debug.LogWarning($"{gameObject.name}: Not currently blocking!");
                return;
            }

            Debug.Log($"{gameObject.name}: Unblocking path...");

            foreach (var node in blockedNodes)
            {
                if (node != null)
                {
                    node.isWalkable = true;
                    Debug.Log($"{gameObject.name}: Unblocked node {node.name}");
                }
            }

            blockedNodes.Clear();
            isBlocking = false;

            // Tell Astar to recalculate all paths and reassign to units
            if (Astar.Instance != null)
            {
                Astar.Instance.RecalculateAndCacheAllPaths();
            }
        }

        private void OnDestroy()
        {
            if (isBlocking)
            {
                UnblockPath();
            }
        }

        private void OnDrawGizmos()
        {
            // Draw detection radius
            Gizmos.color = isBlocking ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Draw blocked nodes
            if (blockedNodes != null && blockedNodes.Count > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                foreach (var node in blockedNodes)
                {
                    if (node != null)
                    {
                        Gizmos.DrawSphere(node.transform.position, 0.3f);
                        Gizmos.DrawLine(transform.position, node.transform.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Show what would be blocked
            if (!isBlocking && nodeLayer != 0)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, nodeLayer);

                foreach (var col in colliders)
                {
                    PathNode node = col.GetComponent<PathNode>();
                    if (node != null)
                    {
                        Gizmos.DrawSphere(node.transform.position, 0.25f);
                    }
                }
            }
        }
    }
}