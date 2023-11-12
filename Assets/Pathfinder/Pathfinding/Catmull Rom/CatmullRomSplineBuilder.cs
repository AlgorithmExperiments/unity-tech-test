using UnityEngine;
using System.Collections.Generic;

public class CatmullRomSplineBuilder : MonoBehaviour
{
    [SerializeField]
    int _numberOfPoints = 36;

    List<PathNode> _splinePath = new();

    bool _showDebuggingGizmos = true;

    public void Reset()
    {
        _splinePath.Clear();
    }



    public List<PathNode> GetSplinePath(List<PathNode> controlPoints)
    {

        //Debug.Log("CATMULL ROM SPLINE BUILDER: GetSplinePath() was called. controlPoints.Count = " + controlPoints.Count);

        if (controlPoints == null || controlPoints.Count < 2)
            return new List<PathNode>(controlPoints);

        Reset();
        
        return CalculatePathPointsAlongCatmullRomSpline(controlPoints);
    }


    List<PathNode> CalculatePathPointsAlongCatmullRomSpline(List<PathNode> controlPoints)
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



    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public void ShowDebuggingGizmos(bool newVisibility) //-------------------------------------
    {
        _showDebuggingGizmos = newVisibility;
    }



    private void OnDrawGizmos()
    {
        if (!_showDebuggingGizmos)
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