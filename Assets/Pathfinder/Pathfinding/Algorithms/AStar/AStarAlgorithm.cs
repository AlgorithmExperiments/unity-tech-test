using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class AStarAlgorithm : MonoBehaviour
{
    [SerializeField] [HideInInspector]
    bool _showDebuggingGizmos = true;


    AStarOpenTilesPriorityDictionary _openTiles = new AStarOpenTilesPriorityDictionary();
    Dictionary<Vector2Int, AStarScoresTile> _closedTiles = new();

    bool _currentlyPathfinding = false;
    bool _abortRequested = false;

    public delegate bool IsTileTraversable(Vector2Int tile);
    IsTileTraversable isTileTraversable;

    public delegate Vector3 GetWorldPositionOfTile(Vector2Int tile);
    GetWorldPositionOfTile getWorldPositionOfTile;

    PathNode[] _finalPath = Array.Empty<PathNode>();

    float startTime;




    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public PathNode[] GetPath(Vector2Int startingTileIndex, Vector2Int destinationTileIndex, Vector2Int tileCountXY, IsTileTraversable isTileTraversable, GetWorldPositionOfTile getWorldPositionOfTile)
    {
        startTime = Time.realtimeSinceStartup;
        
        //💬 Store delegates for use in future method calls
        this.getWorldPositionOfTile = getWorldPositionOfTile; 
        this.isTileTraversable = isTileTraversable;

        if (!isTileTraversable(destinationTileIndex))
        {
            destinationTileIndex = GetTraversableTileNearestToUnreachableTarget(destinationTileIndex, startingTileIndex, tileCountXY);
        }

        //💬 Preparation & Cleanup---------------------------------------------------------------------------------
        _openTiles.Clear();
        _closedTiles.Clear();
        _finalPath = Array.Empty<PathNode>();

        //💬 Preparing startingTile and currentTile----------------------------------------------------------------------
        AStarScoresTile startingTile = new(0m, decimal.MaxValue, decimal.MaxValue, new(-1,-1), startingTileIndex);
        _openTiles.Enqueue(startingTile);
        Vector2Int currentTileIndex = startingTileIndex;


        //💬  A* PATHFINDING ALGORITHM--------------------------------------------------------------------------------
        while (currentTileIndex != destinationTileIndex)
        {
            if (_openTiles.Count == 0 || _abortRequested)
            {
                float currentTime = Time.realtimeSinceStartup;
                //💬---------------------------------------------------------------------------------------
                Debug.Log("A STAR ALGORITHM: GetPath(): BREAK from while() loop: "
                    + (_abortRequested ? "ABORT REQUESTED" : "openTiles.Count == 0")
                    + "Searched " + (_openTiles.Count + _closedTiles.Count)
                    + " tiles in " + (currentTime - startTime) * 1000f + " milliseconds "
                    + "(" + _openTiles.Count + " openTiles and " + _closedTiles.Count + " closedTiles)");
                //-----------------------------------------------------------------------------------------

                _abortRequested = false;
                break;
            }

            //💬 Retrieve tile from open tiles with the LOWEST FScore--------------------------------------
            AStarScoresTile currentTile = _openTiles.Dequeue();
            currentTileIndex = currentTile.Index;
            _closedTiles.Add(currentTileIndex, currentTile);
            
            if (currentTileIndex != destinationTileIndex)
            {
                List<AStarScoresTile> newNeighborScores = GetNewNeighborScores(currentTileIndex, startingTileIndex, destinationTileIndex, tileCountXY, isTileTraversable);
                foreach (AStarScoresTile neighborScoresTile in newNeighborScores)
                {
                    bool neighborAlreadyInOpenTiles =_openTiles.SetGScoreToNewLowerValue(neighborScoresTile.Index, neighborScoresTile.GScore);
                    if (!neighborAlreadyInOpenTiles)
                        _openTiles.Enqueue(neighborScoresTile);
                }
            }
            else
            { 
                break; //💬 Finished; exit while() loop and proceed to final step
            } 
        }
        //💬 COMPILE RESULT-------------------------------------------------------------------------------
        List<Vector2Int> shortestPath = MarchBackwardToCompileFinalResult(destinationTileIndex, startingTileIndex);
        _finalPath = new PathNode[shortestPath.Count];
        for (int i = 0; i < shortestPath.Count; i++)
        {
            _finalPath[i].Position = getWorldPositionOfTile(shortestPath[i]);
        }

        //💬-------------------------------------------------------------------------------------------
        float realtimeSinceStartup = Time.realtimeSinceStartup;
        Debug.Log("A STAR ALGORITHM: GetPath(): Searched " + (_openTiles.Count + _closedTiles.Count) 
            + " tiles in " + (realtimeSinceStartup - startTime)*1000f + " milliseconds  (" 
            + _openTiles.Count + " openTiles and " + _closedTiles.Count + " closedTiles)");
        //----------------------------------------------------------------------------------------------

        return _finalPath;
    }





    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public void AbortAndReset() //--------------------------------------------------------------
    {

        if (_currentlyPathfinding)
            _abortRequested = true;

        //💬 Reset
        _closedTiles.Clear();
        _openTiles.Clear();
        _finalPath = Array.Empty<PathNode>();
    }


    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public void ShowDebuggingGizmos(bool newVisibility) //-------------------------------------
    {
        _showDebuggingGizmos = newVisibility;
    }



    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    List<AStarScoresTile> GetNewNeighborScores(Vector2Int currentTile, Vector2Int startingTile, Vector2Int destinationTile, Vector2Int tileCountXY, IsTileTraversable isTileTraversable)
    {
        //💬 GET NEIGHBORS----------------------------------------------
        Vector2Int minCornerTile = new (
            Math.Max(currentTile.x - 1, 0),
            Math.Max(currentTile.y - 1, 0)
        );
        Vector2Int maxCornerTile = new (
            Math.Min(currentTile.x + 1, tileCountXY.x - 1),
            Math.Min(currentTile.y + 1, tileCountXY.y - 1)
        );
        List<Vector2Int> neighbors = new();
        for (int x = minCornerTile.x; x <= maxCornerTile.x; x++)
        {
            for (int y = minCornerTile.y; y <= maxCornerTile.y; y++)
            {
                Vector2Int neighbor = new(x,y);
                if (isTileTraversable(neighbor) && !_closedTiles.ContainsKey(neighbor))
                    neighbors.Add(neighbor);
            }
        }


        //💬 CALCULATE NEW DISTANCE SCORES-------------------------------------
        List<AStarScoresTile> newNeighborScores = new();
        foreach (Vector2Int neighbor in neighbors)
        {
            decimal gScore = _closedTiles[currentTile].GScore + GetDistanceScore(currentTile, neighbor); // += 1.0 or 1.4
            decimal hScore = GetDistanceScore(neighbor, destinationTile);
            newNeighborScores.Add(new(gScore, hScore, gScore+hScore, currentTile, new(neighbor.x, neighbor.y)));
        }
        return newNeighborScores;
    }





    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    decimal GetDistanceScore(Vector2Int tileStart, Vector2Int tileEnd) //----------------------
    {
        Vector2Int difference = new Vector2Int(Math.Abs(tileEnd.x - tileStart.x), Math.Abs(tileEnd.y - tileStart.y));
        int numberOfDiagonalSteps = Math.Min(difference.x, difference.y);
        int numberOfRemainingNonDiagonalSteps = Math.Max(difference.x, difference.y) - numberOfDiagonalSteps;
        return (1.4m * numberOfDiagonalSteps) + (1.0m * numberOfRemainingNonDiagonalSteps);
    }






    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    Vector2Int GetTraversableTileNearestToUnreachableTarget(Vector2Int targetTile, Vector2Int startingTile, Vector2Int tileCountXY)
    {
        Vector2Int nearestTraversablePerimeterTile = new(-1,-1);

        Vector2Int minCorner = targetTile;
        Vector2Int maxCorner = targetTile;
        HashSet<Vector2Int> perimeterTiles = new();
        HashSet<Vector2Int> winners = new();
        int edgeLength = 1;

        while (winners.Count == 0) {
            maxCorner += Vector2Int.one;
            minCorner -= Vector2Int.one;
            edgeLength += 2;

            perimeterTiles.Clear();

            //💬 GATHER ALL TILES ALONG THE NEW PERIMETER---------------------------------------------------
            for (int i = 0; i < edgeLength - 1; i++)
            {
                perimeterTiles.Add(new (minCorner.x + i, maxCorner.y)); //💬 TOP EDGE (LEFT TO RIGHT)
                perimeterTiles.Add(new (maxCorner.x, maxCorner.y - i)); //💬 RIGHT EDGE (TOP TO BOTTOM)
                perimeterTiles.Add(new (maxCorner.x - i, minCorner.y)); //💬 BOTTOM EDGE (RIGHT TO LEFT)
                perimeterTiles.Add(new (minCorner.x, minCorner.y + i)); //💬 LEFT EDGE (BOTTOM TO TOP)
            }
                
            foreach (Vector2Int tile in perimeterTiles)
            {
                tile.Clamp(Vector2Int.zero, tileCountXY - Vector2Int.one);
                if (isTileTraversable(tile)){
                    winners.Add(tile);
                }
            }
            if (winners.Count != 0)
            {
                decimal minDistanceToStartingTile = decimal.MaxValue;
                
                foreach(Vector2Int tile in winners)
                {
                    decimal distanceToStartingTile = GetDistanceScore(tile, startingTile);
                    if (distanceToStartingTile < minDistanceToStartingTile)
                    {
                        minDistanceToStartingTile = distanceToStartingTile;
                        nearestTraversablePerimeterTile = tile;
                    }
                }
            }
        }
        return nearestTraversablePerimeterTile;
    }





    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    List<Vector2Int> MarchBackwardToCompileFinalResult(Vector2Int destinationTile, Vector2Int startingTile)
    {
        List<Vector2Int> finalPath = new() { destinationTile };
        Vector2Int currentTile = destinationTile;
        while (currentTile != startingTile)
        {
            currentTile = _closedTiles[currentTile].ParentTile;
            finalPath.Add(currentTile);
        }
        finalPath.Reverse();
        return finalPath;
    }






    ///---------------------------------------------------------------------------------------<summary>
    /// Debugging visualizations (only visible when _showDebuggingGizmos is toggled to TRUE).
    /// Requires "draw Gizmos" toggle to be set to ON in the editor viewport. </summary>
    private void OnDrawGizmos() //---------------------------------------------------------------------
    {
        if (getWorldPositionOfTile == null || !_showDebuggingGizmos)
            return;

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Marks openTiles with low opacity and closedTiles with greater opacity:
        float gridTileSize = Mathf.Abs(getWorldPositionOfTile(new(0,0)).x - getWorldPositionOfTile(new(1,0)).x);
        Vector3 rectangleSize = new(1.0f * gridTileSize, 0f, 1.0f * gridTileSize);

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Low opacity for OPEN tiles
        Gizmos.color = new Color(0, 1f, 0.4f, 0.07f); 
        foreach (Vector2Int tile in _openTiles.Keys)
        {
            Gizmos.DrawCube(getWorldPositionOfTile(tile), rectangleSize);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Greater opacity for CLOSED tiles
        Gizmos.color = new Color(0, 1f, 0.5f, 0.15f); 
        foreach (Vector2Int tile in _closedTiles.Keys)
        {
            Gizmos.DrawCube(getWorldPositionOfTile(tile), rectangleSize);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws green circles along highlighted tile path:
        Gizmos.color = new Color(0, 1, 0.3f, 0.25f); //💬 Transparent cyan
        for (int i = 1; i < _finalPath.Length; i++)
        {
            Gizmos.DrawSphere(_finalPath[i].Position, 0.12f);
            Gizmos.DrawLine (_finalPath[i - 1].Position, _finalPath[i].Position);
        }
    }
}
