using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{

    [SerializeField]
    bool _showDebuggingGizmos = true;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    bool _previousShowDebuggingGizmos = true;

    [SerializeField]
    AStarAlgorithm _algorithm;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    AStarAlgorithm _previousAlgorithm;

    [SerializeField] [Range(0.1f, 3f)]
    float _gridTileSize = 1.0f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _previousGridTileSize = 1.0f;
    
    [SerializeField]
    Vector2Int _gridTileCountXY = new(48,48);

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    Vector2Int _previousGridTileCountXY = new(48,48);

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _gridTilesPerPlane = 48.0f; 

    [SerializeField] [Range(0.05f, 8f)]
    float _collisionCheckerHeight = 2f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _previousCollisionCheckerHeight = 2f;

    [SerializeField]
    LayerMask _obstacleLayer = 1 << 4;

    [SerializeField]
    NavGridTile[,] _navigationGrid;

    ///----------------------------------------------------------------------------<summary>
    /// Previous total world size of the navigation grid (after latest trimming)  </summary>
    [HideInInspector] [SerializeField] //👻 HIDDEN - DO NOT EDIT DIRECTLY
    Vector3 _worldSizeOfFittedGrid = new(48, 0, 48); //-------------------------------------


    float _yPlaneHeightOfDebuggingGizmos = 0.04f;


    PathNode[] _finalTilePath = Array.Empty<PathNode>();


    List<Collider> _dynamicObstacles = new();


    //bool _currentlyPathFinding = false;


    //bool _abortRequested = false;


    

    





    void Start()
    {
        //💬
        //Debug.Log("NAV GRID: Start() was called.");

        RepopulateTilesAndResizeMapToPerfectFit();
    }



    void OnValidate()
    {

        if(_algorithm != _previousAlgorithm)
        {
            if(_previousAlgorithm)
                _previousAlgorithm.RequestAbort();
        }
        

        if (!Mathf.Approximately(_collisionCheckerHeight, _previousCollisionCheckerHeight))
        {
            if (!Application.isPlaying)
            {
                _collisionCheckerHeight = _previousCollisionCheckerHeight; //💬 Lock sliderbar if game is not playing
                return;
            }

            _previousCollisionCheckerHeight = _collisionCheckerHeight;
            RecalculateTraversabilityOfEntireGrid();
        }



        if (!Mathf.Approximately(_gridTileSize, _previousGridTileSize))
        {
            if (!Application.isPlaying)
            {
                _gridTileSize = _previousGridTileSize; //💬 Lock sliderbar if game is not playing
                return;
            }

            float collisionPlaneWorldSizeMeters = transform.localScale.x; //We'll assume world map is symmetric in x and y dimensions
            
            _previousGridTileSize = _gridTileSize;
            _gridTilesPerPlane = Mathf.RoundToInt(collisionPlaneWorldSizeMeters/_gridTileSize); //ToDo: Update this to Vector2Int
            
            _gridTileSize = collisionPlaneWorldSizeMeters/_gridTilesPerPlane; 
            _previousGridTileSize = _gridTileSize;
            
            RepopulateTilesAndResizeMapToPerfectFit();
        }

        if (_showDebuggingGizmos != _previousShowDebuggingGizmos)
        {
            _previousShowDebuggingGizmos = _showDebuggingGizmos;
            _algorithm.ShowDebuggingGizmos(_showDebuggingGizmos);
        }


        bool gridTileCountXChanged = _gridTileCountXY.x != _previousGridTileCountXY.x;
        bool gridTileCountYChanged = _gridTileCountXY.y != _previousGridTileCountXY.y;
        if (gridTileCountXChanged || gridTileCountYChanged)
        {
            if (!Application.isPlaying)
            {
                _gridTileCountXY = _previousGridTileCountXY; //💬 Lock values if game is not playing
                return;
            }
            RepopulateTilesAndResizeMapToPerfectFit(true);
        }

    }



    private void Update()
    {
        if (_dynamicObstacles.Count > 0)
        {
            foreach (Collider collider in _dynamicObstacles)
            {
                RecalculateTraversabilityAroundObstacleOnly(collider.bounds);
            }
        }
    }






    public void RegisterObstacle(Collider collider)
    {
        if (!_dynamicObstacles.Contains(collider))
        {
            _dynamicObstacles.Add(collider);
        }
    }



    public void UnregisterObstacle(Collider collider)
    {
        _dynamicObstacles.Remove(collider);
    }



    public bool IsTileTraversable(Vector2Int tile)
    {
        return _navigationGrid[tile.x, tile.y].IsTraversable;
    }



    public Vector3 GetWorldPositionOfTile(Vector2Int index)
    {
        return _navigationGrid[index.x, index.y].CenterPointWorldPosition;
    }


    ///----------------------------------------------------------------------------------------<summary>
    /// Returns the index (x,y) of the nav grid tile located at the indicated world position.
    /// (Assumes ground plane game object is centered at the world's origin point.) </summary>
    public Vector2Int GetIndexOfGridTileAtWorldPosition(Vector3 worldPosition)
    { //---------------------

        Rect gridBounds = new Rect(-_worldSizeOfFittedGrid.x / 2, -_worldSizeOfFittedGrid.z / 2, _worldSizeOfFittedGrid.x, _worldSizeOfFittedGrid.z);
        if (!gridBounds.Contains(new Vector2(worldPosition.x, worldPosition.z)))
        {
            //💬
            //Debug.Log("NAV GRID: GetGridTileAtWorldPosition() Error: target point is out of bounds. Returning grid Tile index dummy value: (-1, -1)");
            return new Vector2Int(-1, -1); // Error: Out of bounds
        }

        Vector2 gridSpacePositionXY = new Vector2(worldPosition.x + (_worldSizeOfFittedGrid.x/2), worldPosition.z + (_worldSizeOfFittedGrid.z/2));
        Vector2Int gridTileIndex = new Vector2Int((int)(gridSpacePositionXY.x / _gridTileSize), (int)(gridSpacePositionXY.y / _gridTileSize));

        //💬
        //Debug.Log("NAV GRID: GetGridTileAtWorldPosition() target point correlates to grid Tile index (" + gridTileIndex.ToString() + ")");
        return gridTileIndex;
    }



    void RepopulateTilesAndResizeMapToPerfectFit(bool hasMapSizeChanged = false)
    {
        //💬
        //Debug.Log("NAV GRID: PopulateTilesAndTrimPlaneToPerfectFit() was called");

        if (hasMapSizeChanged)
            transform.localScale = new (_gridTileCountXY.x*_gridTileSize, 48, _gridTileCountXY.y*_gridTileSize);

        // Calculates tile count:
        _gridTileCountXY = new Vector2Int(
            Mathf.RoundToInt(transform.localScale.x / _gridTileSize),
            Mathf.RoundToInt(transform.localScale.z / _gridTileSize)
        );

        _worldSizeOfFittedGrid = new Vector3(  
            transform.localScale.x,  (/* y = */0) ,
            transform.localScale.z   //💬 When the collision plane gameobject's localScale == 1, it's real-world diameter is 1 x 1 meters:
        );

        _navigationGrid = new NavGridTile[_gridTileCountXY.x, _gridTileCountXY.y];
        
        RecalculateTraversabilityOfEntireGrid();
    }



    void RecalculateTraversabilityOfEntireGrid()
    {
        if (!Application.isPlaying)
        {   //💬
            Debug.Log("NAV GRID: RegenerateTileTraversability() ABORTED: Must be playing in editor or running in standalone");
            return;
        }
        
        //💬
        //Debug.Log("NAV GRID: RegenerateTileTraversability() was called. _navigationGrid size = (" + _navigationGrid.GetLength(0) + ", " + _navigationGrid.GetLength(1) + ")");

        float halfTileSize = _gridTileSize * 0.5f;
        Vector3 tileCollisionCheckBoxExtents = new Vector3(halfTileSize, _collisionCheckerHeight / 2, halfTileSize); // tall box
        Vector3 gridCornerPointOffset = -_worldSizeOfFittedGrid/2;

        for (int x = 0; x < _navigationGrid.GetLength(0); x++)
        {
            for (int y = 0; y < _navigationGrid.GetLength(1); y++)
            {
                Vector3 tileCenter = new Vector3((x + 0.5f) * _gridTileSize, _collisionCheckerHeight / 2, (y + 0.5f) * _gridTileSize);
                tileCenter += gridCornerPointOffset;
                _navigationGrid[x, y].IsTraversable = !Physics.CheckBox(tileCenter, tileCollisionCheckBoxExtents, Quaternion.identity, _obstacleLayer);
                _navigationGrid[x, y].CenterPointWorldPosition = new Vector3( tileCenter.x, _yPlaneHeightOfDebuggingGizmos, tileCenter.z);
            }
        }
    }



    void RecalculateTraversabilityAroundObstacleOnly(Bounds targetObstacleWorldBounds)
    {
        if (!Application.isPlaying)
        {   //💬
            Debug.Log("NAV GRID: RegenerateTileTraversability() ABORTED: Must be playing in editor or running in standalone");
            return;
        }
        
        //💬
        //Debug.Log("NAV GRID: RecalculateTraversabilityOfTilesNearObstacle() was called.");

        float halfTileSize = _gridTileSize * 0.5f;
        Vector3 tileCollisionCheckBoxExtents = new Vector3(halfTileSize, _collisionCheckerHeight / 2, halfTileSize); // tall box
        Vector3 gridCornerPointOffset = -_worldSizeOfFittedGrid/2;
        
        int outerPadding = 2;
        Vector2Int tileMinXY = GetIndexOfGridTileAtWorldPosition(targetObstacleWorldBounds.min);
        Vector2Int tileMaxXY = GetIndexOfGridTileAtWorldPosition(targetObstacleWorldBounds.max);
        int xMin = Mathf.RoundToInt(Mathf.Max(0, tileMinXY.x - outerPadding));
        int yMin = Mathf.RoundToInt(Mathf.Max(0, tileMinXY.y - outerPadding));
        int xMax = Mathf.RoundToInt(Mathf.Min(tileMaxXY.x + outerPadding, _navigationGrid.GetLength(0)));
        int yMax = Mathf.RoundToInt(Mathf.Min(tileMaxXY.y + outerPadding, _navigationGrid.GetLength(1)));
        for (int x = xMin; x < xMax; x++)
        {
            for (int y = yMin; y < yMax; y++)
            {
                Vector3 tileCenter = new Vector3((x + 0.5f) * _gridTileSize, _collisionCheckerHeight / 2, (y + 0.5f) * _gridTileSize);
                tileCenter += gridCornerPointOffset;
                _navigationGrid[x, y].IsTraversable = !Physics.CheckBox(tileCenter, tileCollisionCheckBoxExtents, Quaternion.identity, _obstacleLayer);
                _navigationGrid[x, y].CenterPointWorldPosition = new Vector3( tileCenter.x, _yPlaneHeightOfDebuggingGizmos, tileCenter.z);
            }
        }
    }






    ///---------------------------------------------------------------------------------------<summary>
    /// Given the current and desired location, return a path to the destination.  </summary>
    public PathNode[] GetPath(Vector3 origin, Vector3 destination) //---------------------------
    {
        //💬
        Debug.Log("NAV GRID: GetPath(): _gridTileCountXY = (" + _gridTileCountXY.ToString() + ")");

        _finalTilePath = _algorithm.GetPath(GetIndexOfGridTileAtWorldPosition(origin), GetIndexOfGridTileAtWorldPosition(destination), _gridTileCountXY, IsTileTraversable, GetWorldPositionOfTile);
        return _finalTilePath;
    }



    ///---------------------------------------------------------------------------------------<summary>
    /// Debugging visualizations (only visible when _showDebuggingGizmos is toggled to TRUE).
    /// Requires "draw Gizmos" toggle to be set to ON in the editor viewport. </summary>
    void OnDrawGizmos() //-----------------------------------------------------------------------------
    {
        if (!_showDebuggingGizmos || !Application.isPlaying)
            return;

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws light debugging lines to visualize empty tiles:
        Gizmos.color = new Color(0,0,0,0.08f); //💬 Faint black lines
        int rowCount = _gridTileCountXY.x;
        int columnCount = _gridTileCountXY.y;
        Vector3 gridCornerPointOffset = -_worldSizeOfFittedGrid/2;

        for (int row = 0; row < rowCount; row++) {
            Vector3 startPosition = new (row * _gridTileSize, _yPlaneHeightOfDebuggingGizmos, 0);
            Vector3 endPosition = new (row * _gridTileSize, _yPlaneHeightOfDebuggingGizmos, columnCount * _gridTileSize);
            Gizmos.DrawLine((startPosition + gridCornerPointOffset), (endPosition + gridCornerPointOffset));
        }

        for (int column = 0; column < columnCount; column++) {
            Vector3 startPos = new (0, _yPlaneHeightOfDebuggingGizmos, column * _gridTileSize);
            Vector3 endPos = new (rowCount * _gridTileSize, _yPlaneHeightOfDebuggingGizmos, column * _gridTileSize);
            Gizmos.DrawLine((startPos + gridCornerPointOffset), (endPos + gridCornerPointOffset));
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws red rectangles around tiles containing obstacles:
        Gizmos.color = new Color(1,0,0,0.5f); //💬 Transparent red
        Vector3 rectangleSize = new(1.0f * _gridTileSize, 0f, 1.0f * _gridTileSize);
        foreach (NavGridTile tile in _navigationGrid)
        {
            if (!tile.IsTraversable)
            {
                Gizmos.DrawWireCube(tile.CenterPointWorldPosition, rectangleSize);
            }
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws green circles along highlighted tile path:
        Gizmos.color = new Color(0, 1, 0.3f, 0.25f); //💬 Transparent cyan
        for (int i = 1; i < _finalTilePath.Length; i++)
        {
            Gizmos.DrawSphere(_finalTilePath[i].Position, 0.12f);
            Gizmos.DrawLine (_finalTilePath[i - 1].Position, _finalTilePath[i].Position);
        }
    }

}
