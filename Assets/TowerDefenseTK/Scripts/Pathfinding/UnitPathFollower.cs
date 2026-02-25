using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
	/// <summary>
	/// Handles movement along a path. When a path is blocked or a reroute occurs,
	/// finds the nearest walkable node to the enemy's current position and paths
	/// from there to the exit — so enemies never freeze mid-field.
	/// </summary>
	public class UnitPathFollower : MonoBehaviour
	{
		[Header("Rejoin Settings")]
		[Tooltip("Search radius for finding a nearby walkable node when rejoining after a reroute")]
		[SerializeField] private float rejoinSearchRadius = 5f;

		private List<PathNode> path;
		private int currentIndex = 0;
		private Coroutine followRoutine;
		private MovementComponent movementComp;

		private void OnEnable()
		{
			if (Astar.Instance != null)
				Astar.Instance.RegisterFollower(this);
		}

		private void OnDisable()
		{
			if (Astar.Instance != null)
				Astar.Instance.UnregisterFollower(this);
		}

		public void SetPath(List<PathNode> newPath, float moveSpeed, MovementComponent mc = null)
		{
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
		/// Called by Astar when paths need to be updated due to obstacles.
		/// Tries to rejoin from current world position before falling back to spawn restart.
		/// </summary>
		public void RequestPathUpdate()
		{
			Debug.Log($"{gameObject.name}: Path update requested — attempting rejoin from current position");
			StopMovement();

			if (TryRejoinFromCurrentPosition())
				return;

			// Fallback: restart from spawn via MovementComponent
			if (movementComp != null)
			{
				Debug.LogWarning($"{gameObject.name}: Rejoin failed, falling back to spawn restart");
				movementComp.OnTriggerMove();
			}
			else
			{
				Debug.LogWarning($"{gameObject.name}: Cannot request path update — MovementComponent is null");
			}
		}

		/// <summary>
		/// Find the closest walkable node to our current position, then A* to the nearest exit.
		/// </summary>
		private bool TryRejoinFromCurrentPosition()
		{
			if (Astar.Instance == null) return false;
			if (!NodeGetter.nodeValue.ContainsKey(NodeType.End)) return false;

			PathNode nearest = GetNearestWalkableNode();
			if (nearest == null)
			{
				Debug.LogWarning($"{gameObject.name}: No walkable node found within {rejoinSearchRadius}u");
				return false;
			}

			List<PathNode> bestPath = null;

			foreach (var exitNode in NodeGetter.nodeValue[NodeType.End])
			{
				if (exitNode == null) continue;

				List<PathNode> candidate = Astar.Instance.FindPath(nearest, exitNode);
				if (candidate != null && candidate.Count > 0)
				{
					if (bestPath == null || candidate.Count < bestPath.Count)
						bestPath = candidate;
				}
			}

			if (bestPath == null || bestPath.Count == 0)
			{
				Debug.LogWarning($"{gameObject.name}: No path found from nearest node {nearest.name} to any exit");
				return false;
			}

			// Read speed directly from MovementComponent's public field
			float speed = movementComp != null ? movementComp.movement_Speed : 3f;

			path = bestPath;
			currentIndex = 0;

			StopAllCoroutines();
			followRoutine = StartCoroutine(FollowPath(speed));

			Debug.Log($"{gameObject.name}: Rejoined path from {nearest.name}, {bestPath.Count} nodes to exit");
			return true;
		}

		/// <summary>
		/// Returns the nearest walkable PathNode to this unit's current position.
		/// Uses three strategies in order of cost: direct grid lookup → layer overlap → full node scan.
		/// </summary>
		private PathNode GetNearestWalkableNode()
		{
			// 1. Direct grid lookup via PathNodeGenerator (free, no physics)
			if (PathNodeGenerator.Instance != null)
			{
				PathNode direct = PathNodeGenerator.Instance.GetNodeAtWorldPosition(transform.position);
				if (direct != null && direct.isWalkable)
					return direct;
			}

			// 2. Physics overlap using the same nodeLayer as MovementComponent
			LayerMask nodeLayer = movementComp != null ? movementComp.nodeLayer : default;

			if (nodeLayer != default)
			{
				Collider[] hits = Physics.OverlapSphere(transform.position, rejoinSearchRadius, nodeLayer);
				PathNode nearest = null;
				float nearestDist = float.MaxValue;

				foreach (var hit in hits)
				{
					PathNode node = hit.GetComponent<PathNode>();
					if (node == null || !node.isWalkable) continue;

					float dist = Vector3.Distance(transform.position, node.transform.position);
					if (dist < nearestDist)
					{
						nearestDist = dist;
						nearest = node;
					}
				}

				if (nearest != null) return nearest;
			}

			// 3. Last resort: linear scan of all nodes registered in Astar
			if (Astar.Instance != null)
			{
				PathNode nearest = null;
				float nearestDist = float.MaxValue;

				foreach (var node in Astar.Instance.allNodes)
				{
					if (node == null || !node.isWalkable) continue;
					float dist = Vector3.Distance(transform.position, node.transform.position);
					if (dist < nearestDist)
					{
						nearestDist = dist;
						nearest = node;
					}
				}

				return nearest;
			}

			return null;
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

				if (targetNode == null || !targetNode.isWalkable)
				{
					Debug.LogWarning($"{gameObject.name}: Blocked node at index {currentIndex} — rejoining");
					// Try immediately; Astar will also call RequestPathUpdate when recalculation finishes
					if (!TryRejoinFromCurrentPosition())
						yield break;
					else
						yield break; // TryRejoin started a new coroutine
				}

				Vector3 targetPos = targetNode.transform.position;

				while (Vector3.Distance(transform.position, targetPos) > 0.15f)
				{
					if (!targetNode.isWalkable)
					{
						Debug.LogWarning($"{gameObject.name}: Node became blocked mid-move — rejoining");
						if (!TryRejoinFromCurrentPosition())
							yield break;
						else
							yield break;
					}

					transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
					yield return null;
				}

				currentIndex++;
				if (enemy != null)
					enemy.nodesPassed = currentIndex;
			}

			Debug.Log($"{gameObject.name}: Reached end of path");
		}

		public void StopMovement()
		{
			if (followRoutine != null)
			{
				StopCoroutine(followRoutine);
				followRoutine = null;
			}
		}

		public float GetPathProgress()
		{
			if (path == null || path.Count == 0) return 0f;
			return currentIndex / (float)path.Count;
		}
	}
}