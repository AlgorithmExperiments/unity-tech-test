using System.Collections.Generic;
using UnityEngine;

public class PathSimplifier : MonoBehaviour
{

    LayerMask _obstacleLayer; 

    List<PathNode> _simplifiedPath = new List<PathNode>();

    List<(Vector3, Quaternion, Vector3)> _raycastBoxes = new List<(Vector3, Quaternion, Vector3)>();



    void Start()
    {
        _obstacleLayer = Obstacle.UniversalObstacleLayer;
    }


    public void Reset()
    {
        _raycastBoxes.Clear();
        _simplifiedPath.Clear();
    }

    public List<PathNode> SimplifyPath(PathNode[] controlPoints, Vector2 collisionPostProcessingTunnelBox)
    {
        if (controlPoints == null || controlPoints.Length < 2)
        {
            return new List<PathNode>(controlPoints);
        }

        _raycastBoxes.Clear();
        _simplifiedPath = new List<PathNode>(controlPoints); // Initialize with all control points

        int i = 0;
        while (i < _simplifiedPath.Count - 1)
        {
            bool foundClearPath = false;
            for (int j = _simplifiedPath.Count - 1; j > i; j--)
            {
                if (!IsPathBlocked(_simplifiedPath[i], _simplifiedPath[j], collisionPostProcessingTunnelBox))
                {
                    // Remove points between i and j
                    _simplifiedPath.RemoveRange(i + 1, j - i - 1);
                    foundClearPath = true;
                    break; // Exit the for loop once a clear path is found
                }
            }

            // Increment i if a clear path was found, or if no clear path exists to any farther point
            if (foundClearPath || i + 1 == _simplifiedPath.Count - 1)
            {
                i++;
            }
            else
            {
                // If no clear path is found, move to next point to prevent infinite loop
                i++;
            }
        }

        return _simplifiedPath;
    }


    private bool IsPathBlocked(PathNode start, PathNode end, Vector2 collisionPostProcessingTunnelBox)
    {
        Vector3 direction = end.Position - start.Position;
        float distance = direction.magnitude;
        Vector3 center = start.Position + (direction / 2) + new Vector3(0, (collisionPostProcessingTunnelBox.y/2) + 0.04f , 0);
    
        Vector3 scale = new Vector3(collisionPostProcessingTunnelBox.x, collisionPostProcessingTunnelBox.y, distance);

        Quaternion orientation = Quaternion.LookRotation(direction);
    
        bool isBlocked = Physics.CheckBox(center, scale / 2, orientation, _obstacleLayer);

        if (!isBlocked)
        _raycastBoxes.Add((center, orientation, scale));

        return isBlocked;
    }






    void OnDrawGizmos()
    {
        if (_simplifiedPath != null && _simplifiedPath.Count > 0)
        {
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws cyan line along the simplified path:
            Gizmos.color = Color.cyan; //💬 transparent cyan
            for (int i = 1; i < _simplifiedPath.Count; i++)
            {
                Gizmos.DrawLine (_simplifiedPath[i - 1].Position, _simplifiedPath[i].Position);
            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws yellow boxes along the raycastBoxes:
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); //💬 transparent yellow
            for (int i = 0; i < _raycastBoxes.Count; i++)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(_raycastBoxes[i].Item1, _raycastBoxes[i].Item2, _raycastBoxes[i].Item3);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }

        }
    }
}
