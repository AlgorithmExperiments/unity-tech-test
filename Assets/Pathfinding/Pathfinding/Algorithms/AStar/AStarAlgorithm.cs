using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class AStarAlgorithm : MonoBehaviour
{
    [SerializeField] [HideInInspector]
    bool _showDebuggingGizmos = true;

    AStarScoresTile[,] _aStarScoreTiles;

    HashSet<Vector2Int> _openTiles = new();
    HashSet<Vector2Int> _closedTiles = new();

    bool _currentlyPathfinding = false;
    bool _abortRequested = false;

    public delegate bool IsTileTraversable(Vector2Int tile);
    IsTileTraversable isTileTraversable;

    public delegate Vector3 GetWorldPositionOfTile(Vector2Int tile);
    GetWorldPositionOfTile getWorldPositionOfTile;

    float startTime;




    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public PathNode[] GetPath(Vector2Int startingTile, Vector2Int destinationTile, Vector2Int tileCountXY, IsTileTraversable isTileTraversable, GetWorldPositionOfTile getWorldPositionOfTile)
    {
        startTime = Time.realtimeSinceStartup;
        
        //💬 Store delegates for use in future method calls
        this.getWorldPositionOfTile = getWorldPositionOfTile; 
        this.isTileTraversable = isTileTraversable;

        if (!isTileTraversable(destinationTile))
        {
            destinationTile = GetTraversableTileNearestToUnreachableTarget(destinationTile, startingTile, tileCountXY);
            
            //💬
            Debug.Log("A STAR ALGORITHM: GetPath(): destinationTile was previously unreachable. NEW destinationTile is " 
                + (isTileTraversable(destinationTile) ? "TRAVERSABLE" : "STILL UNREACHABLE"));
        }

        //💬 Preparation & Cleanup---------------------------------------------------------------------------------
        _openTiles.Clear();
        _closedTiles.Clear();
        _aStarScoreTiles = new AStarScoresTile[tileCountXY.x, tileCountXY.y];
        for (int x = 0; x < tileCountXY.x; x++)
        {
            for (int y = 0; y < tileCountXY.y; y++)
            {
                _aStarScoreTiles[x, y] = new AStarScoresTile(decimal.MaxValue, decimal.MaxValue, decimal.MaxValue, new Vector2Int(-1, -1), new Vector2Int(x, y));
            }
        }

        //💬 Preparing startingTile and currentTile----------------------------------------------------------------------
        _openTiles.Add(startingTile); 
        _aStarScoreTiles[startingTile.x, startingTile.y].GScore = 0;
        _aStarScoreTiles[startingTile.x, startingTile.y].HScore = GetDistanceScore(startingTile, destinationTile);
        _aStarScoreTiles[startingTile.x, startingTile.y].FScore = _aStarScoreTiles[startingTile.x, startingTile.y].HScore;
        Vector2Int currentTile = startingTile;


        //💬  A* PATHFINDING ALGORITHM--------------------------------------------------------------------------------
        while (currentTile != destinationTile)
        {
            if (_openTiles.Count == 0  ||  _abortRequested)
            {
                //💬---------------------------------------------------------------------------------------
                Debug.Log("A STAR ALGORITHM: GetPath(): BREAK from while() loop: " 
                    + (_abortRequested ? "ABORT REQUESTED" : "openTiles.Count == 0")
                    + "Searched " + (_openTiles.Count + _closedTiles.Count) 
                    + " tiles in " + (Time.realtimeSinceStartup - startTime)*1000f + " milliseconds " 
                    + "(" + _openTiles.Count + " openTiles and " + _closedTiles.Count + " closedTiles)");
                //-----------------------------------------------------------------------------------------
                
                _abortRequested = false;
                break;
            }

            //💬 Find LOWEST FScore------------------------------------------------------------------------
            currentTile = FindLowestFScoreInOpenTiles();
            _closedTiles.Add(currentTile);
            _openTiles.Remove(currentTile);

            if (currentTile != destinationTile)
            {
                List<AStarScoresTile> newNeighborScores = GetNewNeighborScores(currentTile, startingTile, destinationTile, tileCountXY, isTileTraversable);
                foreach (AStarScoresTile scoresTile in newNeighborScores)
                {
                    Vector2Int index = scoresTile.Index;
                    if (scoresTile.GScore < _aStarScoreTiles[index.x, index.y].GScore || !_openTiles.Contains(index))
                    {
                        _aStarScoreTiles[index.x, index.y] = scoresTile;
                        if (!_openTiles.Contains(scoresTile.Index))
                            _openTiles.Add(scoresTile.Index);
                    }
                }
            }
            else
            { 
                break; //💬 Finished; exit while() loop and proceed to final step
            } 
        }
        //💬 COMPILE RESULT-------------------------------------------------------------------------------
        List<Vector2Int> shortestPath = MarchBackwardToCompileFinalResult(destinationTile, startingTile);
        PathNode[] shortestPath3D = new PathNode[shortestPath.Count];
        for (int i = 0; i < shortestPath.Count; i++)
        {
            shortestPath3D[i].Position = getWorldPositionOfTile(shortestPath[i]);
        }

        //💬-------------------------------------------------------------------------------------------
        Debug.Log("A STAR ALGORITHM: GetPath(): Searched " + (_openTiles.Count + _closedTiles.Count) 
            + " tiles in " + (Time.realtimeSinceStartup - startTime)*1000f + " milliseconds  (" 
            + _openTiles.Count + " openTiles and " + _closedTiles.Count + " closedTiles)");
        //----------------------------------------------------------------------------------------------

        return shortestPath3D;
    }




    ///-------------------------------------------------------------------------------<summary>
    /// Description here... </summary>
    public void RequestAbort() //--------------------------------------------------------------
    {
        if (_currentlyPathfinding)
            _abortRequested = true;
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
                if (isTileTraversable(neighbor) && !_closedTiles.Contains(neighbor))
                    neighbors.Add(neighbor);
            }
        }


        //💬 CALCULATE NEW DISTANCE SCORES-------------------------------------
        List<AStarScoresTile> newNeighborScores = new();
        foreach (Vector2Int neighbor in neighbors)
        {
            decimal gScore = _aStarScoreTiles[currentTile.x, currentTile.y].GScore + GetDistanceScore(currentTile, neighbor);
            decimal hScore = GetDistanceScore(neighbor, destinationTile);
            Vector2Int index = new(neighbor.x, neighbor.y);
            newNeighborScores.Add(new(gScore, hScore, gScore+hScore, currentTile, index));
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
    Vector2Int FindLowestFScoreInOpenTiles()
    {
        if (_openTiles.Count == 0)
        {
            //💬
            Debug.LogError("A STAR ALGORITHM: FindLowestFScoreInOpenTiles(): ERROR: _openTiles is empty; returning dummy value (-1,-1)");
            RequestAbort();
            return new(-1, -1);
        }
        decimal lowestFScore = decimal.MaxValue;
        decimal lowestHScoreTieBreaker = decimal.MaxValue; //💬 Tie-breaker when deciding between identical lowestFScores
        Vector2Int tileWithLowestFScore = new(-1, -1);
        foreach (Vector2Int tile in _openTiles)
        {
            decimal fScore = _aStarScoreTiles[tile.x, tile.y].FScore;
            decimal hScore = _aStarScoreTiles[tile.x, tile.y].HScore;
            if (fScore < lowestFScore || (fScore == lowestFScore && hScore < lowestHScoreTieBreaker))
            {
                //💬 New tile wins out!
                    tileWithLowestFScore = tile;
                    lowestFScore = fScore;
                    lowestHScoreTieBreaker = hScore;
            }
        }
        return tileWithLowestFScore;
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
        //💬
        Debug.Log("A STAR ALGORITHM: MarchBackwardToCollateFinalResult(): destinationTile = " + destinationTile.ToString() + ", startingTile = " + startingTile.ToString());

        List<Vector2Int> finalPath = new() { destinationTile };
        Vector2Int currentTile = destinationTile;
        while (currentTile != startingTile)
        {
            currentTile = _aStarScoreTiles[currentTile.x, currentTile.y].ParentTile;
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

        //💬 Low opacity for OPEN tiles
        Gizmos.color = new Color(0, 1f, 0.4f, 0.07f); 
        foreach (Vector2Int tile in _openTiles)
        {
            Gizmos.DrawCube(getWorldPositionOfTile(tile), rectangleSize);
        }

        //💬 Greater opacity for CLOSED tiles
        Gizmos.color = new Color(0, 1f, 0.5f, 0.15f); 
        foreach (Vector2Int tile in _closedTiles)
        {
            Gizmos.DrawCube(getWorldPositionOfTile(tile), rectangleSize);
        }
    }
}
