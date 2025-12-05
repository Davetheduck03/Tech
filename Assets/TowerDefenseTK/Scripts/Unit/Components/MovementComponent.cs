using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Purely handles initiating movement. All pathfinding is handled by Astar.
    /// </summary>
    public class MovementComponent : UnitComponent
    {
        public float movement_Speed;
        private UnitPathFollower agent;
        public LayerMask nodeLayer;
        public bool isFlying;

        [Tooltip("Assign a Transform that represents the movement goal (e.g. the end waypoint).")]
        public Transform targetTransform;

        protected override void OnInitialize()
        {
            movement_Speed = data.Speed;
            agent = GetComponent<UnitPathFollower>();

            var enemy = GetComponent<BaseEnemy>();
            if (enemy != null) enemy.isFlying = isFlying;
        }

        private PathNode GetCurrentNode()
        {
            return NodeGetter.GetNodeBelow(transform.position + Vector3.up * 0.5f, nodeLayer);
        }

        private PathNode GetTargetNode()
        {
            return NodeGetter.GetNodeBelow(targetTransform.position + Vector3.up * 0.5f, nodeLayer);
        }

        /// <summary>
        /// Request a path from Astar and start movement
        /// </summary>
        public void OnTriggerMove()
        {
            if (Astar.Instance == null)
            {
                Debug.LogError($"{gameObject.name}: Astar instance not found!");
                return;
            }

            PathNode start = GetCurrentNode();
            PathNode goal = GetTargetNode();

            if (start == null)
            {
                Debug.LogWarning($"{gameObject.name}: Could not find start node at position {transform.position}");
                return;
            }

            if (goal == null)
            {
                Debug.LogWarning($"{gameObject.name}: Could not find goal node at position {targetTransform.position}");
                return;
            }

            // Request path from Astar - it will handle cache checking and validation
            List<PathNode> path = Astar.Instance.GetPath(start, goal);

            if (path != null && path.Count > 0)
            {
                // Find the nearest node on the path to current position
                PathNode nearestNode = FindNearestNodeOnPath(path);
                int startIndex = path.IndexOf(nearestNode);

                // Create a subpath starting from the nearest node
                List<PathNode> adjustedPath = path.GetRange(startIndex, path.Count - startIndex);

                agent.SetPath(adjustedPath, movement_Speed, this);
                Debug.Log($"{gameObject.name}: Path assigned from nearest node {nearestNode.name} (index {startIndex}) to {goal.name}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: No valid path from {start.name} to {goal.name}");
            }
        }

        /// <summary>
        /// Find the nearest node on the path to the current position
        /// </summary>
        private PathNode FindNearestNodeOnPath(List<PathNode> path)
        {
            if (path == null || path.Count == 0) return null;

            PathNode nearestNode = path[0];
            float nearestDistance = Vector3.Distance(transform.position, nearestNode.transform.position);

            for (int i = 1; i < path.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, path[i].transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNode = path[i];
                }
            }

            return nearestNode;
        }
    }
}