using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Purely handles movement along a path. All pathfinding logic is in Astar.
    /// </summary>
    public class UnitPathFollower : MonoBehaviour
    {
        private List<PathNode> path;
        private int currentIndex = 0;
        private Coroutine followRoutine;
        private MovementComponent movementComp;

        private void OnEnable()
        {
            // Register with Astar for path updates
            if (Astar.Instance != null)
            {
                Astar.Instance.RegisterFollower(this);
            }
        }

        private void OnDisable()
        {
            // Unregister from Astar
            if (Astar.Instance != null)
            {
                Astar.Instance.UnregisterFollower(this);
            }
        }

        public void SetPath(List<PathNode> newPath, float moveSpeed, MovementComponent mc = null)
        {
            // Only set path if it's valid
            if (newPath == null || newPath.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: Received invalid path!");
                return;
            }

            path = newPath;
            currentIndex = 0;
            movementComp = mc;

            StopAllCoroutines();
            followRoutine = StartCoroutine(FollowPath(moveSpeed));

            Debug.Log($"{gameObject.name}: Following path with {path.Count} nodes");
        }

        /// <summary>
        /// Called by Astar when paths need to be updated due to obstacles
        /// </summary>
        public void RequestPathUpdate()
        {
            if (movementComp == null)
            {
                Debug.LogWarning($"{gameObject.name}: Cannot request path update - MovementComponent is null");
                return;
            }

            Debug.Log($"{gameObject.name}: Path update requested by Astar");

            // Stop current movement
            StopMovement();

            // Request new path from MovementComponent
            movementComp.OnTriggerMove();
        }

        private IEnumerator FollowPath(float moveSpeed)
        {
            var enemy = GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.totalPathNodes = path.Count;
                enemy.nodesPassed = 0;
            }

            while (currentIndex < path.Count)
            {
                PathNode targetNode = path[currentIndex];

                // Safety check - if node becomes unwalkable, stop immediately
                if (targetNode == null || !targetNode.isWalkable)
                {
                    Debug.LogWarning($"{gameObject.name}: Encountered blocked node at index {currentIndex}. Stopping movement.");
                    yield break; // Stop moving, wait for Astar to reassign path
                }

                Vector3 targetPos = targetNode.transform.position;

                // Move towards the target node
                while (Vector3.Distance(transform.position, targetPos) > 0.15f)
                {
                    // Double-check during movement that target is still walkable
                    if (!targetNode.isWalkable)
                    {
                        Debug.LogWarning($"{gameObject.name}: Target node became blocked during movement!");
                        yield break;
                    }

                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                // Reached this node, move to next
                currentIndex++;
                if (enemy != null)
                    enemy.nodesPassed = currentIndex;
            }

            Debug.Log($"{gameObject.name}: Reached end of path");
        }

        /// <summary>
        /// Stop current movement
        /// </summary>
        public void StopMovement()
        {
            if (followRoutine != null)
            {
                StopCoroutine(followRoutine);
                followRoutine = null;
            }
        }

        /// <summary>
        /// Get current progress along path (0-1)
        /// </summary>
        public float GetPathProgress()
        {
            if (path == null || path.Count == 0) return 0f;
            return currentIndex / (float)path.Count;
        }
    }
}