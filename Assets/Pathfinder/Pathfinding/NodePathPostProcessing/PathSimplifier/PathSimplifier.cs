using System.Collections.Generic;
using UnityEngine;

public class PathSimplifier : NodePathPostProcessor
{

    [SerializeField] [Range(0,4)]
    float _nearbyMergeRadius = 1.5f;

    [SerializeField] [Range(1,6)]
    float _collisionBoxScalar = 1.5f;

    LayerMask _obstacleLayer; 

    List<PathNode> _simplifiedPath = new List<PathNode>();

    List<(Vector3, Quaternion, Vector3)> _gizmoCollisionTunnelBoxes = new List<(Vector3, Quaternion, Vector3)>();
    List<(Vector3, Vector3)> _gizmoRadialProjectionLines = new List<(Vector3, Vector3)>();
    List<(Vector3, Quaternion, Vector3)> _gizmoRadialProjectionHits = new List<(Vector3, Quaternion, Vector3)>();
    List<Vector3> _gizmoNewlyAdjustedNodePositions = new List<Vector3>();



    void Start()
    {
        _obstacleLayer = Obstacle.UniversalObstacleLayer;
    }


    public override void Reset()
    {
        _gizmoCollisionTunnelBoxes.Clear();
        _gizmoRadialProjectionLines.Clear();
        _gizmoRadialProjectionHits.Clear();
        _gizmoNewlyAdjustedNodePositions.Clear();
        _simplifiedPath.Clear();
    }




    public override List<PathNode> GetNewPath(List<PathNode> controlPoints, Vector3 collisionBoxSize)
    {
        if (controlPoints == null || controlPoints.Count < 2)
            return new List<PathNode>(controlPoints);

        Reset();
        
        return CalculateSimplifiedPathPoints(controlPoints, collisionBoxSize);
    }



    List<PathNode> CalculateSimplifiedPathPoints(List<PathNode> controlPoints, Vector3 collisionTunnelBoxSize)
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            return new List<PathNode>(controlPoints);
        }

        Reset();
        _simplifiedPath = new List<PathNode>(controlPoints); // Initialize a copy of all control points

        int i = 0;
        while (i < _simplifiedPath.Count - 1)
        {
            bool foundClearPath = false;
            for (int j = _simplifiedPath.Count - 1; j > i; j--)
            {
                if (!IsPathBlocked(_simplifiedPath[i], _simplifiedPath[j], collisionTunnelBoxSize))
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

        
        FindAndMergeNodesWithinSmallRadiusApart(collisionTunnelBoxSize);

        List<int> indicesOfNodesToAdjust = new();
        for (int index = 1; index < _simplifiedPath.Count - 1; index++) {
            indicesOfNodesToAdjust.Add(index);
        }
            
        BufferPointsAwayFromNeighboringObstacles(indicesOfNodesToAdjust, collisionTunnelBoxSize);

        return _simplifiedPath;
    }




    ///--------------------------------------------------------------------------------------<summary>
    /// Perform collision tunneling to determine if path is blocked by a collision mesh with
    /// layer type == _obstacleLayer </summary>
    private bool IsPathBlocked(PathNode start, PathNode end, Vector3 collisionTunnelBoxSize)
    {
        Vector3 direction = end.Position - start.Position;
        float distance = direction.magnitude;
        Vector3 center = start.Position + (direction / 2) + new Vector3(0, (collisionTunnelBoxSize.y/2) + 0.04f , 0);
    
        Vector3 scale = new Vector3(collisionTunnelBoxSize.x, collisionTunnelBoxSize.y, distance);

        Quaternion orientation = Quaternion.LookRotation(direction);
    
        bool isBlocked = Physics.CheckBox(center, scale / 2, orientation, _obstacleLayer);

        if (!isBlocked)
            _gizmoCollisionTunnelBoxes.Add((center, orientation, scale));

        return isBlocked;
    }



    void FindAndMergeNodesWithinSmallRadiusApart(Vector3 collisionTunnelBoxSize)
    {
        List<int> indicesOfNewlyMergedNodes = new List<int>();

        int i = 2;
        while (i < _simplifiedPath.Count)
        {
            if ((_simplifiedPath[i].Position - _simplifiedPath[i - 1].Position).sqrMagnitude < (_nearbyMergeRadius * _nearbyMergeRadius))
            {
                if (IsPathBlocked(_simplifiedPath[i], _simplifiedPath[i - 2], collisionTunnelBoxSize))
                {
                    BufferPointsAwayFromNeighboringObstacles(new List<int>() { i }, collisionTunnelBoxSize);
                    if (!IsPathBlocked(_simplifiedPath[i], _simplifiedPath[i - 2], collisionTunnelBoxSize))
                    {
                        _simplifiedPath.RemoveAt(i - 1);
                        continue;
                    }
                }
            }
            i++;
        }

        for (int index = 1; index < _simplifiedPath.Count - 1; index++)
        {
            indicesOfNewlyMergedNodes.Add(index);
        }
        return;
    }



    void BufferPointsAwayFromNeighboringObstacles(List<int> indicesOfNodesToAdjust, Vector3 collisionTunnelBoxSize)
    {
        Vector3 modifiedCollisionBoxSize = new Vector3(collisionTunnelBoxSize.x * _collisionBoxScalar, collisionTunnelBoxSize.y, collisionTunnelBoxSize.z * _collisionBoxScalar);
        Vector3 largeCollisionBoxSizeExtents = new Vector3(modifiedCollisionBoxSize.x * _collisionBoxScalar, modifiedCollisionBoxSize.y, modifiedCollisionBoxSize.x * _collisionBoxScalar) / 2;
        Vector3 radialProjectionCollisionBoxSizeExtents = new Vector3(modifiedCollisionBoxSize.x / 12, modifiedCollisionBoxSize.y / 2, modifiedCollisionBoxSize.x / 4);

        //(Position, Rotation)[]
        (Vector3, Quaternion)[] secondaryCollisionBoxOffsets = new (Vector3, Quaternion)[]
        {
            //💬 0 - CenterTop: 
            (new Vector3(0, modifiedCollisionBoxSize.y/2, modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 0, 0)),
            //💬 1 - TopRight: 
            (new Vector3(0.707f*modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, 0.707f*modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 45, 0)),
            //💬 2 - CenterRight: 
            (new Vector3(modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, 0), Quaternion.Euler(0, 90, 0)),
            //💬 3 - BottomRight: 
            (new Vector3(0.707f*modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, -0.707f*modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 135, 0)),
            //💬 4 - CenterBottom: 
            (new Vector3(0, modifiedCollisionBoxSize.y/2, -modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 180, 0)),
            //💬 5 - BottomLeft: 
            (new Vector3(-0.707f*modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, -0.707f*modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 225, 0)),
            //💬 6 - CenterLeft: 
            (new Vector3(-modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, 0), Quaternion.Euler(0, 270, 0)),
            //💬 7 - TopLeft: 
            (new Vector3(-0.707f*modifiedCollisionBoxSize.x/4, modifiedCollisionBoxSize.y/2, 0.707f*modifiedCollisionBoxSize.z/4), Quaternion.Euler(0, 315, 0)),
        };


        List<(int, Vector3)> updatedNodes = new();
        //💬 Perform a radial projection with 8 directional Physics checkboxes if near an obstacle
        //foreach (int nodeIndex in indicesOfNewlyMergedNodes)
        foreach (int index in indicesOfNodesToAdjust)
        {
            //Vector3 nodeCenter = _simplifiedPath[nodeIndex].Position
            Vector3 nodeCenter = _simplifiedPath[index].Position;
            //💬 Skip radial projection if no obstacles are nearby:
            if (Physics.CheckBox(nodeCenter, largeCollisionBoxSizeExtents, Quaternion.identity, _obstacleLayer))
            {
                //💬 Proceed to perform 8 radially-projected Physics Checkboxes
                List<Vector3> collisionVectors = new();
                foreach ((Vector3, Quaternion) offsets in secondaryCollisionBoxOffsets)
                {
                    /* 💬 DEBUGGING GIZMO: */ _gizmoRadialProjectionLines.Add((nodeCenter, nodeCenter + offsets.Item1 - new Vector3(0, modifiedCollisionBoxSize.y / 2, 0)));
                    if (Physics.CheckBox(nodeCenter + offsets.Item1, radialProjectionCollisionBoxSizeExtents, offsets.Item2, _obstacleLayer))
                    {
                        collisionVectors.Add(new Vector3(offsets.Item1.x, 0, offsets.Item1.z));
                        /* 💬 DEBUGGING GIZMO: */
                        _gizmoRadialProjectionHits.Add((nodeCenter + offsets.Item1 - new Vector3(0, modifiedCollisionBoxSize.y / 2, 0), offsets.Item2, new Vector3(radialProjectionCollisionBoxSizeExtents.x / 8, 0.01f, radialProjectionCollisionBoxSizeExtents.z / 2)));
                    }
                }
                //💬 Sum up collision offset vectors
                Vector3 sum = Vector3.zero;
                foreach (Vector3 collisionVector in collisionVectors)
                    sum += collisionVector;

                //💬 Add averaged collision offset vector if accumulated collisions to provide shift node away from obstacles
                if(collisionVectors.Count != 0)
                {
                    Vector3 newPosition = _simplifiedPath[index].Position - (sum / collisionVectors.Count);
                    updatedNodes.Add((index, newPosition));
                    /* 💬 DEBUGGING GIZMO: */ _gizmoNewlyAdjustedNodePositions.Add(newPosition);
                }
                //🏆
                //_simplifiedPath[i] = new PathNode(newPosition);
            }
        }
        foreach ((int, Vector3) updatedNode in updatedNodes) {
            _simplifiedPath[updatedNode.Item1] = new PathNode(updatedNode.Item2);
        }
    }



    




    void OnDrawGizmos()
    {
        if (!_showPostProcessingVisualizations)
            return;

        if (_simplifiedPath != null && _simplifiedPath.Count > 0)
        {
            //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws cyan line along highlighted tile path:
            Gizmos.color = new Color(0, 1, 0.3f, 0.5f); //💬 Transparent cyan
            for (int i = 1; i < _simplifiedPath.Count; i++)
            {
                Gizmos.DrawSphere(_simplifiedPath[i].Position, 0.08f);
                Gizmos.DrawLine (_simplifiedPath[i - 1].Position, _simplifiedPath[i].Position);
            }
            //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws pink points at new node positions:
            Gizmos.color = new Color(1, 0.4f, 1f, 0.7f); //💬 Transparent pink
            for (int i = 0; i < _gizmoNewlyAdjustedNodePositions.Count; i++)
            {
                Gizmos.DrawSphere(_gizmoNewlyAdjustedNodePositions[i], 0.12f);
            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws yellow lines as symbolic standins for radial projection checkboxes:
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f); //💬 transparent yellow
            for (int i = 0; i < _gizmoRadialProjectionLines.Count; i++)
            {
                Gizmos.DrawLine(_gizmoRadialProjectionLines[i].Item1, _gizmoRadialProjectionLines[i].Item2);
            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws red rectangles for radial projection checkboxes which resulted in hits:
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); //💬 transparent red
            for (int i = 0; i < _gizmoRadialProjectionHits.Count; i++)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(_gizmoRadialProjectionHits[i].Item1, _gizmoRadialProjectionHits[i].Item2, _gizmoRadialProjectionHits[i].Item3);
                Gizmos.matrix = matrix;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws yellow boxes along the collisionTunnelBoxes:
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); //💬 transparent yellow
            for (int i = 0; i < _gizmoCollisionTunnelBoxes.Count; i++)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(_gizmoCollisionTunnelBoxes[i].Item1, _gizmoCollisionTunnelBoxes[i].Item2, _gizmoCollisionTunnelBoxes[i].Item3);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }
}
