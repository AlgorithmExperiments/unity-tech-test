using UnityEngine;
using System.Collections.Generic;

///------------------------------------------------------------------------------<summary>
/// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
public class CatmullRomSplineBuilder : NodePathPostProcessor
{
    [SerializeField]
    int _numberOfPoints = 36;

    List<PathNode> _splinePath = new();


    public override void Reset()
    {
        _splinePath.Clear();
    }





    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Generates a Catmull-Rom spline fitted to the control points, and returns
    /// a new set of path points fitted to the new spline. </summary>
    protected override List<PathNode> ApplyPostProcessingToNodePath(List<PathNode> controlPoints, Vector3 collisionBoxSize) //--
    {
        _splinePath = new List<PathNode>();

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector3 p0 = (i == 0) ? controlPoints[i].Position : controlPoints[i - 1].Position;
            Vector3 p1 = controlPoints[i].Position;
            Vector3 p2 = controlPoints[i + 1].Position;
            Vector3 p3 = (i == controlPoints.Count - 2) ? controlPoints[i + 1].Position : controlPoints[i + 2].Position;

            for (int j = 0; j < _numberOfPoints; j++)
            {
                float t = j / (float)_numberOfPoints;
                Vector3 point = CalculateCatmullRomPoint(t, p0, p1, p2, p3);
                _splinePath.Add(new PathNode(point));
            }
        }
        return _splinePath;
    }


    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
    private Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float a = -0.5f * t3 + t2 - 0.5f * t;
        float b = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float c = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float d = 0.5f * t3 - 0.5f * t2;

        return a * p0 + b * p1 + c * p2 + d * p3;
    }





    private void OnDrawGizmos()
    {
        if (!_showPostProcessingVisualizations)
            return;

        if (_splinePath != null && _splinePath.Count > 0)
        {
            //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //💬 Draws purple line along highlighted tile path:
            Gizmos.color = new Color(1f, 0.1f, 0.9f, 1f); //💬 purple
            for (int i = 1; i < _splinePath.Count; i++)
            {
                //Gizmos.DrawSphere(_splinePath[i].Position, 0.12f);
                Gizmos.DrawLine (_splinePath[i - 1].Position, _splinePath[i].Position);
            }
        }
    }
    
}