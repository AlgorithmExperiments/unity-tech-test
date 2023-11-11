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
    float _tileSize = 1.0f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _previousTileSize = 1.0f;
    
    [SerializeField]
    Vector2Int _gridTileCountXY = new(48,48);

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    Vector2Int _previousGridTileCountXY = new(48,48);

    [SerializeField] [Range(0f, 3f)]
    float _obstacleCollisionPadding = 0f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _previousObstacleCollisionPadding = 0f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _gridTilesPerPlane = 48.0f; 

    [SerializeField] [Range(0.5f, 3f)]
    float _collisionPostProcessingTunnelWidth = 1f;

    [SerializeField] [Range(0.05f, 8f)]
    float _collisionCheckerHeight = 2f;

    [HideInInspector] [SerializeField] //🔒 HIDDEN - DO NOT EDIT DIRECTLY
    float _previousCollisionCheckerHeight = 2f;

    [SerializeField]
    LayerMask _obstacleLayer;

    [SerializeField]
    NavGridTile[,] _navGridTiles;

    ///----------------------------------------------------------------------------<summary>
    /// Previous total world size of the navigation grid (after latest trimming)  </summary>
    [HideInInspector] [SerializeField] //👻 HIDDEN - DO NOT EDIT DIRECTLY
    Vector3 _worldSizeOfFittedGrid = new(48, 0, 48); //-------------------------------------


    float _yPlaneHeightOfDebuggingGizmos = 0.04f;


    PathNode[] _finalTilePath = Array.Empty<PathNode>();


    List<Collider> _dynamicObstacles = new();








    void Start()
    {
        _obstacleLayer = Obstacle.UniversalObstacleLayer;
        RepopulateTilesAndResizeMapToPerfectFit();
    }





    private void Update()
    {
        if (_dynamicObstacles.Count > 0)
        {
            foreach (Collider collider in _dynamicObstacles)
            {
                UpdateTilesAroundObstacleInMotion(collider.bounds);
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
        return _navGridTiles[tile.x, tile.y].IsTraversable;
    }



    public Vector3 GetWorldPositionOfTile(Vector2Int index)
    {
        return _navGridTiles[index.x, index.y].CenterPointWorldPosition;
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
        Vector2Int gridTileIndex = new Vector2Int((int)(gridSpacePositionXY.x / _tileSize), (int)(gridSpacePositionXY.y / _tileSize));

        //💬
        //Debug.Log("NAV GRID: GetGridTileAtWorldPosition() target point correlates to grid Tile index (" + gridTileIndex.ToString() + ")");
        return gridTileIndex;
    }



    void RepopulateTilesAndResizeMapToPerfectFit(bool hasMapSizeChanged = false)
    {

        if (hasMapSizeChanged)
            transform.localScale = new (_gridTileCountXY.x*_tileSize, 48, _gridTileCountXY.y*_tileSize);

        // Calculates tile count:
        _gridTileCountXY = new Vector2Int(
            Mathf.RoundToInt(transform.localScale.x / _tileSize),
            Mathf.RoundToInt(transform.localScale.z / _tileSize)
        );

        _worldSizeOfFittedGrid = new Vector3(  
            transform.localScale.x,  (/* y = */0) ,
            transform.localScale.z   //💬 When the collision plane gameobject's localScale == 1, it's real-world diameter is 1 x 1 meters:
        );

        _navGridTiles = new NavGridTile[_gridTileCountXY.x, _gridTileCountXY.y];
        
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

        float halfTileSize = _tileSize * 0.5f;
        float obstacleCollisionPaddingInMeters = _tileSize*_obstacleCollisionPadding;
        Vector3 tileCollisionCheckBoxExtents = new Vector3(halfTileSize + obstacleCollisionPaddingInMeters, _collisionCheckerHeight / 2, halfTileSize + obstacleCollisionPaddingInMeters); // tall box
        Vector3 gridCornerPointOffset = -_worldSizeOfFittedGrid/2;

        for (int x = 0; x < _navGridTiles.GetLength(0); x++)
        {
            for (int y = 0; y < _navGridTiles.GetLength(1); y++)
            {
                Vector3 tileCenter = new Vector3((x + 0.5f) * _tileSize, _collisionCheckerHeight / 2, (y + 0.5f) * _tileSize);
                tileCenter += gridCornerPointOffset;
                _navGridTiles[x, y].IsTraversable = !Physics.CheckBox(tileCenter, tileCollisionCheckBoxExtents, Quaternion.identity, _obstacleLayer);
                _navGridTiles[x, y].CenterPointWorldPosition = new Vector3( tileCenter.x, _yPlaneHeightOfDebuggingGizmos, tileCenter.z);
            }
        }
    }



    void UpdateTilesAroundObstacleInMotion(Bounds targetObstacleWorldBounds)
    {
        if (!Application.isPlaying)
        {   //💬
            Debug.Log("NAV GRID: RegenerateTileTraversability() ABORTED: Must be playing in editor or running in standalone");
            return;
        }
        
        float halfTileSize = _tileSize * 0.5f;
        float obstacleCollisionPaddingInMeters = _tileSize*_obstacleCollisionPadding;
        Vector3 tileCollisionCheckBoxExtents = new Vector3(halfTileSize + obstacleCollisionPaddingInMeters, _collisionCheckerHeight / 2, halfTileSize + obstacleCollisionPaddingInMeters); // tall box
        Vector3 gridCornerPointOffset = -_worldSizeOfFittedGrid/2;
        
        int outerPadding = 2;
        Vector2Int tileMinXY = GetIndexOfGridTileAtWorldPosition(targetObstacleWorldBounds.min);
        Vector2Int tileMaxXY = GetIndexOfGridTileAtWorldPosition(targetObstacleWorldBounds.max);
        int xMin = Mathf.RoundToInt(Mathf.Max(0, tileMinXY.x - outerPadding));
        int yMin = Mathf.RoundToInt(Mathf.Max(0, tileMinXY.y - outerPadding));
        int xMax = Mathf.RoundToInt(Mathf.Min(tileMaxXY.x + outerPadding, _navGridTiles.GetLength(0)));
        int yMax = Mathf.RoundToInt(Mathf.Min(tileMaxXY.y + outerPadding, _navGridTiles.GetLength(1)));
        for (int x = xMin; x < xMax; x++)
        {
            for (int y = yMin; y < yMax; y++)
            {
                Vector3 tileCenter = new Vector3((x + 0.5f) * _tileSize, _collisionCheckerHeight / 2, (y + 0.5f) * _tileSize);
                tileCenter += gridCornerPointOffset;
                _navGridTiles[x, y].IsTraversable = !Physics.CheckBox(tileCenter, tileCollisionCheckBoxExtents, Quaternion.identity, _obstacleLayer);
                _navGridTiles[x, y].CenterPointWorldPosition = new Vector3( tileCenter.x, _yPlaneHeightOfDebuggingGizmos, tileCenter.z);
            }
        }
    }






    ///---------------------------------------------------------------------------------------<summary>
    /// Given the current and desired location, return a path to the destination.  </summary>
    public PathNode[] GetPath(Vector3 origin, Vector3 destination) //---------------------------
    {
        _finalTilePath = _algorithm.GetPath(GetIndexOfGridTileAtWorldPosition(origin), GetIndexOfGridTileAtWorldPosition(destination), _gridTileCountXY, IsTileTraversable, GetWorldPositionOfTile, new(_collisionPostProcessingTunnelWidth, _collisionCheckerHeight));
        return _finalTilePath;
    }






    void OnValidate()
    {
        
        if(_algorithm != _previousAlgorithm) {
            if (_previousAlgorithm != null) {
                _previousAlgorithm.AbortAndReset();
            }
            _previousAlgorithm = _algorithm;
        }
        else if (_algorithm != null) {
            _algorithm.AbortAndReset();
        }

        

        if (!Mathf.Approximately(_collisionCheckerHeight, _previousCollisionCheckerHeight))
        {
            if (!Application.isPlaying) {
                _collisionCheckerHeight = _previousCollisionCheckerHeight; //💬 Lock sliderbar if game is not playing
                return;
            }
            _previousCollisionCheckerHeight = _collisionCheckerHeight;
            RecalculateTraversabilityOfEntireGrid();
        }


        if(!Mathf.Approximately(_obstacleCollisionPadding, _previousObstacleCollisionPadding))
        {
            if (!Application.isPlaying) {
                _obstacleCollisionPadding = _previousObstacleCollisionPadding; //💬 Lock sliderbar if game is not playing
                return;
            }
            _previousObstacleCollisionPadding = _obstacleCollisionPadding;
            RecalculateTraversabilityOfEntireGrid();
        }



        if (!Mathf.Approximately(_tileSize, _previousTileSize))
        {
            if (!Application.isPlaying) {
                _tileSize = _previousTileSize; //💬 Lock sliderbar if game is not playing
                return;
            }
            float collisionPlaneWorldSizeMeters = transform.localScale.x; //We'll assume world map is symmetric in x and y dimensions
            _gridTilesPerPlane = Mathf.RoundToInt(collisionPlaneWorldSizeMeters/_tileSize); //ToDo: Update this to Vector2Int
            _tileSize = collisionPlaneWorldSizeMeters/_gridTilesPerPlane; 
            _previousTileSize = _tileSize;
            RepopulateTilesAndResizeMapToPerfectFit();
        }



        if (_showDebuggingGizmos != _previousShowDebuggingGizmos) {
            _previousShowDebuggingGizmos = _showDebuggingGizmos;
            _algorithm.ShowDebuggingGizmos(_showDebuggingGizmos);
        }



        bool gridTileCountXChanged = _gridTileCountXY.x != _previousGridTileCountXY.x;
        bool gridTileCountYChanged = _gridTileCountXY.y != _previousGridTileCountXY.y;
        if (gridTileCountXChanged || gridTileCountYChanged)
        {
            if (!Application.isPlaying) {
                _gridTileCountXY = _previousGridTileCountXY; //💬 Lock values if game is not playing
                return;
            }
            RepopulateTilesAndResizeMapToPerfectFit(true);
        }
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
            Vector3 startPosition = new (row * _tileSize, _yPlaneHeightOfDebuggingGizmos, 0);
            Vector3 endPosition = new (row * _tileSize, _yPlaneHeightOfDebuggingGizmos, columnCount * _tileSize);
            Gizmos.DrawLine((startPosition + gridCornerPointOffset), (endPosition + gridCornerPointOffset));
        }

        for (int column = 0; column < columnCount; column++) {
            Vector3 startPos = new (0, _yPlaneHeightOfDebuggingGizmos, column * _tileSize);
            Vector3 endPos = new (rowCount * _tileSize, _yPlaneHeightOfDebuggingGizmos, column * _tileSize);
            Gizmos.DrawLine((startPos + gridCornerPointOffset), (endPos + gridCornerPointOffset));
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws red rectangles around tiles containing obstacles:
        Gizmos.color = new Color(1,0,0,0.5f); //💬 Transparent red
        Vector3 rectangleSize = new(1.0f * _tileSize, 0f, 1.0f * _tileSize);
        foreach (NavGridTile tile in _navGridTiles)
        {
            if (!tile.IsTraversable)
            {
                Gizmos.DrawWireCube(tile.CenterPointWorldPosition, rectangleSize);
            }
        }
    }

}
