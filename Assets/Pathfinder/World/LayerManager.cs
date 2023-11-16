using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    
    

    [SerializeField]
    int _defaultTerrainLayer = 0;

    [SerializeField]
    int _defaultObstacleLayer = 4;

    public static LayerMask DefaultObstacleLayerMask;

    public static LayerMask DefaultTerrainLayerMask;

    public static int DefaultTerrainLayer = 0;

    public static int DefaultObstacleLayer = 4;
    


    void OnValidate()
    {
        UpdateLayerValues();
    }

    void Awake()
    {
        UpdateLayerValues();
    }


    void UpdateLayerValues()
    {
        DefaultObstacleLayer = _defaultObstacleLayer;
        DefaultTerrainLayer = _defaultTerrainLayer;

        DefaultObstacleLayerMask = 1 << _defaultObstacleLayer;
        DefaultTerrainLayerMask = 1 << _defaultTerrainLayer;
    }
}


