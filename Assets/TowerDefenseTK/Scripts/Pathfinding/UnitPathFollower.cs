using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPathFollower : MonoBehaviour
{
    private List<PathNode> path;
    private int currentIndex = 0;


    public void SetPath(List<PathNode> newPath, float moveSpeed)
    {
        path = newPath;
        currentIndex = 0;
        StopAllCoroutines();

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
            if (currentIndex >= path.Count)
                yield break;
        }

        
    }
}
