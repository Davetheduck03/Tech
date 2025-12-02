using UnityEngine;

public class GridNode
{
    public Vector2Int coords;
    public bool occupied;
    public Vector3 worldPos;

    public GridNode(Vector2Int c, Vector3 pos)
    {
        coords = c;
        worldPos = pos;
        occupied = false;
    }
}