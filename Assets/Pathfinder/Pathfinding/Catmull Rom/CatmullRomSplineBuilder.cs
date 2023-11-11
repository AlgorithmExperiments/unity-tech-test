using UnityEngine;
using System.Collections.Generic;

public class CatmullRomSplineBuilder
{
    public int numberOfPoints = 50;

    public List<Vector3> GetSpline(Vector3[] controlPoints)
    {
        if (controlPoints.Length < 2) return new List<Vector3>();

        List<Vector3> splinePoints = CalculateCatmullRomSpline(new List<Vector3>(controlPoints), numberOfPoints);
        return splinePoints;
    }

    private List<Vector3> CalculateCatmullRomSpline(List<Vector3> points, int numberOfPoints)
    {
        List<Vector3> splinePoints = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? points[i] : points[i - 1];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = i == points.Count - 2 ? points[i + 1] : points[i + 2];

            for (int j = 0; j < numberOfPoints; j++)
            {
                float t = j / (float)numberOfPoints;
                Vector3 point = CalculateCatmullRomPoint(t, p0, p1, p2, p3);
                splinePoints.Add(point);
            }
        }

        return splinePoints;
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
}