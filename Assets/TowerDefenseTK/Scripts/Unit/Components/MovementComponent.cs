using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MovementComponent : UnitComponent
{
    private float movement_Speed;
    private UnitPathFollower agent;

    [SerializeField] PathNode startNode;
    [SerializeField] PathNode endNode;

    [Tooltip("Assign a Transform that represents the movement goal (e.g. the end waypoint).")]
    public Transform targetTransform;

    protected override void OnInitialize()
    {
        movement_Speed = data.Speed;
        agent = this.gameObject.GetComponent<UnitPathFollower>();
    }

    public void OnTriggerMove()
    {
        List<PathNode> path = Astar.Instance.FindPath(startNode, endNode);
        agent.SetPath(path, movement_Speed);
    }
}
