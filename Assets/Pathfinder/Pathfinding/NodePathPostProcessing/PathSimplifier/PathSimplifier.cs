using System;
using System.Collections.Generic;
using UnityEngine;

public class PathSimplifier : NodePathPostProcessor
{

    [SerializeField] [Range(2f,4)]
    float _nearbyMergeRadius = 2.5f;

    //[SerializeField] [Range(1,6)]
    float _collisionBoxScalar = 2.0f;

    [Tooltip("Breaking into reasonable segments can help prevent distortions artifacts in spline curves")]
    [SerializeField] [Range(10,20)]
    float _maxSegmentLength = 10f;

    [Tooltip("0 interpolates newly inserted points linearly along subdivided segments, 1 interpolates with a bezier curve.")]
    [SerializeField] [Range(0.1f, 0.3f)]
    float _interpolationCurveFactor = 0.2f;

    LayerMask _obstacleLayerMask; 

    List<PathNode> _simplifiedPath = new List<PathNode>();

    List<(Vector3, Quaternion, Vector3)> _gizmoCollisionTunnelBoxes = new List<(Vector3, Quaternion, Vector3)>();
    List<(Vector3, Vector3)> _gizmoRadialProjectionLines = new List<(Vector3, Vector3)>();
    List<(Vector3, Quaternion, Vector3)> _gizmoRadialProjectionHits = new List<(Vector3, Quaternion, Vector3)>();
    List<Vector3> _gizmoNewlyAdjustedNodePositions = new List<Vector3>();
    float _yPlaneHeightOfDebuggingGizmos = 0.06f;

    public override void Reset()
    {
        _gizmoCollisionTunnelBoxes.Clear();
        _gizmoRadialProjectionLines.Clear();
        _gizmoRadialProjectionHits.Clear();
        _gizmoNewlyAdjustedNodePositions.Clear();
        _simplifiedPath.Clear();
    }






    protected override List<PathNode> ApplyPostProcessingToNodePath(List<PathNode> controlPoints, Vector3 collisionTunnelBoxSize)
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            return new List<PathNode>(controlPoints);
        }

        Reset();
        _obstacleLayerMask = LayerManager.DefaultObstacleLayerMask;
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

        List<int> indicesOfNodesToRepelAwayFromObstacles = new();
        for (int index = 1; index < _simplifiedPath.Count - 1; index++) {
            indicesOfNodesToRepelAwayFromObstacles.Add(index);
        }
            
        RepelPointsAwayFromNeighboringObstacles(indicesOfNodesToRepelAwayFromObstacles, collisionTunnelBoxSize);

        List<int> indicesOfNewlyInsertedNodes = BreakUpLongerSegmentsIfNeeded(_interpolationCurveFactor);

        RepelPointsAwayFromNeighboringObstacles(indicesOfNewlyInsertedNodes, collisionTunnelBoxSize);

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
    
        bool isBlocked = Physics.CheckBox(center, scale / 2, orientation, _obstacleLayerMask);

        if (!isBlocked)
            _gizmoCollisionTunnelBoxes.Add((center, orientation, scale));

        return isBlocked;
    }



    void FindAndMergeNodesWithinSmallRadiusApart(Vector3 collisionTunnelBoxSize)
    {
        List<int> indicesOfNewlyMergedNodes = new List<int>();

        int n = 2;
        while (n < _simplifiedPath.Count)
        {
            if ((_simplifiedPath[n].Position - _simplifiedPath[n - 1].Position).sqrMagnitude < (_nearbyMergeRadius * _nearbyMergeRadius))
            {
                if (IsPathBlocked(_simplifiedPath[n], _simplifiedPath[n - 2], collisionTunnelBoxSize))
                {
                    RepelPointsAwayFromNeighboringObstacles(new List<int>() { n }, collisionTunnelBoxSize);
                    if (!IsPathBlocked(_simplifiedPath[n], _simplifiedPath[n - 2], collisionTunnelBoxSize))
                    {
                        //💬 Averages the position and removes the node at (n-1)
                        _simplifiedPath[n] = new PathNode((_simplifiedPath[n].Position + _simplifiedPath[n-1].Position) / 2);
                        _simplifiedPath.RemoveAt(n - 1);
                        continue;
                    }
                }
            }
            n++;
        }

        for (int index = 1; index < _simplifiedPath.Count - 1; index++)
        {
            indicesOfNewlyMergedNodes.Add(index);
        }
        return;
    }



    void RepelPointsAwayFromNeighboringObstacles(List<int> indicesOfNodesToAdjust, Vector3 collisionTunnelBoxSize)
    {
        Vector3 modifiedCollisionBoxSize = new Vector3(collisionTunnelBoxSize.x * _collisionBoxScalar, collisionTunnelBoxSize.y, collisionTunnelBoxSize.z * _collisionBoxScalar);
        Vector3 largeCollisionBoxSizeExtents = new Vector3(modifiedCollisionBoxSize.x / 2, modifiedCollisionBoxSize.y / 2, modifiedCollisionBoxSize.x / 2);
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
            if (Physics.CheckBox(nodeCenter, largeCollisionBoxSizeExtents, Quaternion.identity, _obstacleLayerMask))
            {
                //💬 Proceed to perform 8 radially-projected Physics Checkboxes
                List<Vector3> collisionVectors = new();
                foreach ((Vector3, Quaternion) offsets in secondaryCollisionBoxOffsets)
                {
                    /* 💬 DEBUGGING GIZMO: */ _gizmoRadialProjectionLines.Add((nodeCenter + _yPlaneHeightOfDebuggingGizmos*Vector3.up, new Vector3((nodeCenter.x + (2*offsets.Item1).x), _yPlaneHeightOfDebuggingGizmos, (nodeCenter.z + (2*offsets.Item1).z))));
                    if (Physics.CheckBox(nodeCenter + offsets.Item1, radialProjectionCollisionBoxSizeExtents, offsets.Item2, _obstacleLayerMask))
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


    ///-------------------------------------------------------------------------------<summary>
    /// Returns the indices of any new nodes inserted into the NodePath. </summary>
    List<int> BreakUpLongerSegmentsIfNeeded() //----------------------------------------------
    {
        List<int> indicesOfNewNodes = new();
        Vector3 nodeOffsetDistanceFromPrevious;
        float segmentLength;
        Vector3 subSegmentLengthVector;
        for (int n = 1; n < _simplifiedPath.Count; n++)
        {
            nodeOffsetDistanceFromPrevious = _simplifiedPath[n].Position - _simplifiedPath[n-1].Position;

            //💬 QUICK COMPARE (Assumes segmentLengthVector.magnitude > 1)
            if (Vector3.SqrMagnitude(nodeOffsetDistanceFromPrevious) > _maxSegmentLength)
            {
                segmentLength = Vector3.Magnitude(nodeOffsetDistanceFromPrevious);
                int numberOfNewNodesToInsert = Mathf.FloorToInt(segmentLength/_maxSegmentLength);
                PathNode[] newNodes = new PathNode[numberOfNewNodesToInsert];
                subSegmentLengthVector = nodeOffsetDistanceFromPrevious/(numberOfNewNodesToInsert + 1);
                for (int i = 0; i < numberOfNewNodesToInsert; i++) {
                    newNodes[i] = new(_simplifiedPath[n-1].Position + ((i + 1) * (subSegmentLengthVector)));
                    indicesOfNewNodes.Add(n + i);
                }
                _simplifiedPath.InsertRange(n, newNodes);
                n += numberOfNewNodesToInsert;
            }
        }
        return indicesOfNewNodes;
    }



    ///-------------------------------------------------------------------------------<summary>
    /// Returns the indices of any new nodes inserted into the NodePath. </summary>
    List<int> BreakUpLongerSegmentsIfNeeded(float curveFactor) //----------------------------------------------
    {
        List<int> indicesOfNewNodes = new();
        Vector3 nodeOffsetDistanceFromPrevious;
        float segmentLength;
        for (int n = 1; n < _simplifiedPath.Count; n++)
        {
            nodeOffsetDistanceFromPrevious = _simplifiedPath[n].Position - _simplifiedPath[n-1].Position;

            //💬 QUICK COMPARE (Assumes segmentLengthVector.magnitude > 1)
            if (Vector3.SqrMagnitude(nodeOffsetDistanceFromPrevious) > _maxSegmentLength)
            {
                segmentLength = Vector3.Magnitude(nodeOffsetDistanceFromPrevious);
                int numberOfNewNodesToInsert = Mathf.FloorToInt(segmentLength/_maxSegmentLength);
                Vector3[] surroundingNodes = new Vector3[4];

                //-------------------------------------------------
                //💬    [n-2]   [n-1]                [n]   [n+1]
                //💬      o       o       . . .       o      o
                //💬    NODE    START   (newNodes)   END    NODE
                //💬    BEFORE                              AFTER
                //💬    START                               END
                //-------------------------------------------------
                bool hasPreviousNodeBeforeStartPoint = (n - 2 > 0);
                bool hasAdditionalNodeAfterEndPoint =  (n + 1 < _simplifiedPath.Count);
                if (hasPreviousNodeBeforeStartPoint) {
                    surroundingNodes[0] = _simplifiedPath[n-2].Position;
                    surroundingNodes[1] = _simplifiedPath[n-1].Position;
                }
                else {
                    surroundingNodes[0] = _simplifiedPath[n-1].Position;
                    surroundingNodes[1] = Vector3.Lerp(_simplifiedPath[n-1].Position, _simplifiedPath[n].Position, 0.1f);
                }
                if (hasAdditionalNodeAfterEndPoint) {
                    surroundingNodes[2] = _simplifiedPath[n].Position;
                    surroundingNodes[3] = _simplifiedPath[n+1].Position;
                }
                else {
                    surroundingNodes[2] = Vector3.Lerp(_simplifiedPath[n].Position, _simplifiedPath[n-1].Position, 0.1f);
                    surroundingNodes[3] = _simplifiedPath[n].Position;
                }

                List<PathNode> nodesToInsert = GenerateNodesAlongBezierCurveBetweenPoints(surroundingNodes, numberOfNewNodesToInsert, _interpolationCurveFactor);
                for (int i = 0; i< nodesToInsert.Count; i++) {
                    indicesOfNewNodes.Add(n + i);
                }
                _simplifiedPath.InsertRange(n, nodesToInsert);
                n += nodesToInsert.Count;
            }
        }
        return indicesOfNewNodes;
    }




    public List<PathNode> GenerateNodesAlongBezierCurveBetweenPoints(Vector3[] originalFourPoints, int numberOfPointsToInsert, float curveFactor)
    {
        List<PathNode> nodesToBeInserted = new List<PathNode>();

        if (originalFourPoints.Length != 4 || numberOfPointsToInsert <= 0)
        {
            return nodesToBeInserted; // Return the original path if conditions aren't met
        }

        for (int index = 1; index <= numberOfPointsToInsert; index++)
        {
            float t = index / (float)(numberOfPointsToInsert + 1);
            Vector3 straightLinePoint = Vector3.Lerp(originalFourPoints[1], originalFourPoints[2], t);
            Vector3 bezierPoint = CalculateBezierPoint(t, originalFourPoints[0], originalFourPoints[1], originalFourPoints[2], originalFourPoints[3]);
            Vector3 curvedPoint = Vector3.Lerp(straightLinePoint, bezierPoint, curveFactor);

            nodesToBeInserted.Add(new(curvedPoint));
        }

        //for (int i = 0; i < numberOfPointsToInsert; i++)
        //{
            //float t = (i + 1) / (float)(numberOfPointsToInsert + 1); // Normalized position of the new point

            //// Linearly interpolated point (straight line)
            //Vector3 linearPoint = Vector3.Lerp(originalFourPoints[1], originalFourPoints[2], t);

            //// Quadratic Bezier curve calculation
            //Vector3 bezierPoint1 = Vector3.Lerp(originalFourPoints[1], originalFourPoints[0], t);
            //Vector3 bezierPoint2 = Vector3.Lerp(originalFourPoints[0], originalFourPoints[2], t);
            //Vector3 curvePoint = Vector3.Lerp(bezierPoint1, bezierPoint2, t);

            // Lerp between straight line and curve based on curveFactor
            //Vector3 finalPoint = Vector3.Lerp(linearPoint, curvePoint, curveFactor);

            //nodesToBeInserted.Add(new(finalPoint)); // Inserting at index 2, accounting for previously inserted points
        //}

        return nodesToBeInserted;
    }



    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 p1Trajectory = p1 + (p1 - p0);
        Vector3 p2Trajectory = p2 + (p2 - p3);
        p0 = p1;
        p3 = p2;
        p1 = p1Trajectory;
        p2 = p2Trajectory;
        

        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);
        Vector3 c = Vector3.Lerp(p2, p3, t);
        Vector3 d = Vector3.Lerp(a, b, t);
        Vector3 e = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(d, e, t);
    }

    //private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    //{
    //    float u = 1 - t;
    //    float tt = t * t;
    //    float uu = u * u;
    //    float uuu = uu * u;
    //    float ttt = tt * t;

    //    Vector3 point = uuu * p0; // Influence of p0
    //    point += 3 * uu * t * p1; // Influence of p1
    //    point += 3 * u * tt * p2; // Influence of p2
    //    point += ttt * p3; // Influence of p3

    //    return point;
    //}

    private void Start()
    {
        Vector3[] originalFour = new Vector3[4] 
        {
            new Vector3(0,0,0),
            new Vector3(1,1,0),
            new Vector3(3,1,0),
            new Vector3(4,0,0)
        };
        List<PathNode> newTestPoints = GenerateNodesAlongBezierCurveBetweenPoints(originalFour, 3, 1f);
        Debug.Log($"PATH SIMPLIFIER: (0,0) (1,1) ({newTestPoints[0].Position.x:F1},{newTestPoints[0].Position.y:F1}) ({newTestPoints[1].Position.x:F1},{newTestPoints[1].Position.y:F1}) ({newTestPoints[2].Position.x:F1},{newTestPoints[2].Position.y:F1}) (3,1) (4,0)");
    }


    




    void OnDrawGizmos()
    {
        if (!_showPostProcessingVisualizations)
            return;

        if (_simplifiedPath != null && _simplifiedPath.Count > 0)
        {
            //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws green line along highlighted tile path:
            Gizmos.color = new Color(0f, 1f, 0f, 1f); //💬 solid green
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
            //💬 Draws green lines as symbolic standins for radial projection checkboxes:
            Gizmos.color = new Color(0f, 1f, 0f, 1f); //💬 green
            for (int i = 0; i < _gizmoRadialProjectionLines.Count; i++)
            {
                Gizmos.DrawLine(_gizmoRadialProjectionLines[i].Item1, _gizmoRadialProjectionLines[i].Item2);
            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws red rectangles for radial projection checkboxes which resulted in hits:
            Gizmos.color = new Color(1f, 0f, 0f, 1f); //💬 red
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
