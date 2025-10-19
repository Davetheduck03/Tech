using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TowerDefenseTK
{
    public class MovementComponent : UnitComponent
    {
        private float movement_Speed;
        private UnitPathFollower agent;
        [SerializeField] private LayerMask nodeLayer;


        [Tooltip("Assign a Transform that represents the movement goal (e.g. the end waypoint).")]
        public Transform targetTransform;

        protected override void OnInitialize()
        {
            movement_Speed = data.Speed;
            agent = this.gameObject.GetComponent<UnitPathFollower>();
        }


        public void OnTriggerMove()
        {
            agent.SetPath(Astar.Instance.pathCache[(NodeGetter.GetNodeBelow(transform.position + Vector3.up * 0.5f, nodeLayer), NodeGetter.nodeValue[NodeType.End].First())], movement_Speed);
        }
    }
}
