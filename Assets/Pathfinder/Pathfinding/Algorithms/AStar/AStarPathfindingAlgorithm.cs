using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;


public class AStarPathfindingAlgorithm : PathfindingAlgorithm
{   

    List<PathNode> _rawAStarPath = new List<PathNode>();

    AStarOpenTilesPriorityDictionary _neighborTiles = new AStarOpenTilesPriorityDictionary();

    Dictionary<Vector2Int, AStarScoresTile> _investigatedTiles = new();

    
    



    protected override List<PathNode> CalculatePathfinding(Vector2Int startingTileIndex, Vector2Int destinationTileIndex, Vector2Int tileCountXY, Vector3 collisionBoxSize)
    {
        _startTime = Time.realtimeSinceStartup;

        if (!_isTileTraversable(destinationTileIndex)) {
            destinationTileIndex = GetTraversableTileNearestToUnreachableTarget(destinationTileIndex, startingTileIndex, tileCountXY);
        }

        //💬 Preparation & Cleanup---------------------------------------------------------------------------------
        Reset();

        //💬 Prepare startingTile and currentTile----------------------------------------------------------------------
        AStarScoresTile startingTile = new(0m, decimal.MaxValue, decimal.MaxValue, new(-1,-1), startingTileIndex);
        _neighborTiles.Enqueue(startingTile);
        Vector2Int currentTileIndex = startingTileIndex;


        //💬 MAIN LOGIC LOOP--------------------------------------------------------------------------------
        while (currentTileIndex != destinationTileIndex)
        {
            if (_neighborTiles.Count == 0 || _abortRequested)
            {
                //💬-DEBUG.LOG()--------------------------------------------------------------------------
                Debug.Log("A STAR ALGORITHM: GetPath(): ABORTING - broke from while() loop. Reason: " + (_abortRequested ? "ABORT REQUESTED" : "neighborTiles.Count == 0"));
                //-----------------------------------------------------------------------------------------
                _abortRequested = false; //💬 clean-up
                break;
            }

            //💬 SELECTS THE NEXT TILE (LOWEST F-SCORE)--------------------------------------
            AStarScoresTile currentTile = _neighborTiles.Dequeue();
            //💬 ADDS IT TO THE COMPLETED-- SET OF TILES-------------------------------------
            currentTileIndex = currentTile.Index;
            _investigatedTiles.Add(currentTileIndex, currentTile);
            
            //💬 ADDS ANY NEW NEIGHBORS------------------------------------------------------
            if (currentTileIndex != destinationTileIndex)
            {
                List<AStarScoresTile> newNeighborScores = GetNewNeighborScores(currentTileIndex, startingTileIndex, destinationTileIndex, tileCountXY);
                foreach (AStarScoresTile neighborScoresTile in newNeighborScores)
                {
                    bool neighborAlreadyInOpenTiles =_neighborTiles.SetGScoreToNewLowerValue(neighborScoresTile.Index, neighborScoresTile.GScore);
                    if (!neighborAlreadyInOpenTiles)
                        _neighborTiles.Enqueue(neighborScoresTile);
                }
            }
            else
            { 
                break; //💬 FINISHED; Exit while() loop and proceed to final step
            } 
        }
        //💬 COMPILE RESULT-------------------------------------------------------------------------------
        List<Vector2Int> shortestPath = MarchBackwardToCompileFinalResult(destinationTileIndex, startingTileIndex);
        for (int i = 0; i < shortestPath.Count; i++)
        {
            _rawAStarPath.Add(new PathNode(_getWorldPositionOfTile(shortestPath[i])));
        }

        //💬-DEBUG.LOG()--------------------------------------------------------------------------------
        float realtimeSinceStartup = Time.realtimeSinceStartup;
        Debug.Log("A STAR ALGORITHM: GetPath(): Searched " + (_neighborTiles.Count + _investigatedTiles.Count) 
            + " tiles in " + (realtimeSinceStartup - _startTime)*1000f + " milliseconds  (" 
            + _neighborTiles.Count + " openTiles and " + _investigatedTiles.Count + " closedTiles)");
        //----------------------------------------------------------------------------------------------

        return _rawAStarPath;
    }



    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
    protected override void ResetData() //--------------------------------------------------------------------
    {
        _investigatedTiles.Clear();
        _neighborTiles.Clear();
        _rawAStarPath.Clear();
    }




    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
    List<AStarScoresTile> GetNewNeighborScores(Vector2Int currentTile, Vector2Int startingTile, Vector2Int destinationTile, Vector2Int tileCountXY)
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
                if (_isTileTraversable(neighbor) && !_investigatedTiles.ContainsKey(neighbor))
                    neighbors.Add(neighbor);
            }
        }


        //💬 CALCULATE NEW DISTANCE SCORES-------------------------------------
        List<AStarScoresTile> newNeighborScores = new();
        foreach (Vector2Int neighbor in neighbors)
        {
            decimal gScore = _investigatedTiles[currentTile].GScore + GetDistanceScore(currentTile, neighbor); // += 1.0 or 1.4
            decimal hScore = GetDistanceScore(neighbor, destinationTile);
            newNeighborScores.Add(new(gScore, hScore, gScore+hScore, currentTile, new(neighbor.x, neighbor.y)));
        }
        return newNeighborScores;
    }





    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
    decimal GetDistanceScore(Vector2Int tileStart, Vector2Int tileEnd) //----------------------
    {
        Vector2Int difference = new Vector2Int(Math.Abs(tileEnd.x - tileStart.x), Math.Abs(tileEnd.y - tileStart.y));
        int numberOfDiagonalSteps = Math.Min(difference.x, difference.y);
        int numberOfRemainingNonDiagonalSteps = Math.Max(difference.x, difference.y) - numberOfDiagonalSteps;
        return (1.4m * numberOfDiagonalSteps) + (1.0m * numberOfRemainingNonDiagonalSteps);
    }






    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
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
                if (_isTileTraversable(tile)){
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





    ///------------------------------------------------------------------------------<summary>
    /// 🔭 Nothing here yet... ✨   (description coming soon)   </summary>
    List<Vector2Int> MarchBackwardToCompileFinalResult(Vector2Int destinationTile, Vector2Int startingTile)
    {
        List<Vector2Int> finalPath = new() { destinationTile };
        Vector2Int currentTile = destinationTile;
        while (currentTile != startingTile)
        {
            currentTile = _investigatedTiles[currentTile].ParentTile;
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
        if (_getWorldPositionOfTile == null || !_showAlgorithmVisualizations)
            return;

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Marks openTiles with low opacity and closedTiles with greater opacity:
        float gridTileSize = Mathf.Abs(_getWorldPositionOfTile(new(0,0)).x - _getWorldPositionOfTile(new(1,0)).x);
        Vector3 rectangleSize = new(1.0f * gridTileSize, 0f, 1.0f * gridTileSize);

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Low opacity for OPEN tiles
        Gizmos.color = new Color(0, 1f, 0.4f, 0.07f); 
        foreach (Vector2Int tile in _neighborTiles.Keys)
        {
            Gizmos.DrawCube(_getWorldPositionOfTile(tile), rectangleSize);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Greater opacity for CLOSED tiles
        Gizmos.color = new Color(0, 1f, 0.5f, 0.15f); 
        foreach (Vector2Int tile in _investigatedTiles.Keys)
        {
            Gizmos.DrawCube(_getWorldPositionOfTile(tile), rectangleSize);
        }
        //// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
        //💬 Draws green line along highlighted tile path:
        Gizmos.color = new Color(0, 0.8f, 0f, 0.25f); //💬 Transparent green
        for (int i = 1; i < _rawAStarPath.Count; i++)
        {
            Gizmos.DrawLine (_rawAStarPath[i - 1].Position, _rawAStarPath[i].Position);
        }
    }
}
