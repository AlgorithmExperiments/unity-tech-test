using UnityEngine;

public struct PathNode
{
    /// <summary>
    /// World position of the node
    /// </summary>
    public Vector3 Position;


    public PathNode(Vector3 Position)
    {
        this.Position = Position;
    }
}