using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{

    public class UnitPathFollower : MonoBehaviour
    {
        private List<PathNode> path;
        private int currentIndex = 0;
        private Coroutine followRoutine;

        public void SetPath(List<PathNode> newPath, float moveSpeed)
        {
            path = newPath;
            currentIndex = 0;
            StopAllCoroutines();

            if (followRoutine != null)
                StopCoroutine(followRoutine);

            if (path != null && path.Count > 0)
                StartCoroutine(FollowPath(moveSpeed));
        }

        private IEnumerator FollowPath(float moveSpeed)
        {
            while (currentIndex < path.Count)
            {
                PathNode targetNode = path[currentIndex];
                Vector3 targetPos = targetNode.transform.position;

                while (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                currentIndex++;
            }
        }

        /// <summary>
        /// Recalculates path from current position to target node.
        /// </summary>
        public void RecalculatePath(PathNode newGoal)
        {
            MovementComponent movementComponent = this.GetComponent<MovementComponent>();
            PathNode currentNode = NodeGetter.GetNodeBelow(transform.position + Vector3.up * 0.5f, movementComponent.nodeLayer);
            if (currentNode == null || newGoal == null) return;

            var newPath = Astar.Instance.FindPath(currentNode, newGoal);
            SetPath(newPath, movementComponent.movement_Speed);
        }
    }
}
