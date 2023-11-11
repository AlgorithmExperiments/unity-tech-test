using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AStarOpenTilesPriorityDictionary
{

    private Utils.PriorityQueue<AStarScoresTile, (decimal, decimal)> _openTilesPriorityQueue;
    private Dictionary<Vector2Int, (decimal gScore, AStarScoresTile tile)> _updatedTileGScores;

    public int Count
    {
        //💬 The dictionary's Count serves as the source of truth since the priority queue can contain leftover duplicates
        get { return _updatedTileGScores.Count; }
    }

    public IEnumerable<Vector2Int> Keys
    {
        get { return _updatedTileGScores.Keys; }
    }


    public AStarOpenTilesPriorityDictionary()
    {
        _openTilesPriorityQueue = new Utils.PriorityQueue<AStarScoresTile, (decimal, decimal)>(new AStarScoresTileComparer());
        _updatedTileGScores = new Dictionary<Vector2Int, (decimal, AStarScoresTile)>();
    }



    public void Clear()
    {
        _openTilesPriorityQueue.Clear();
        _updatedTileGScores.Clear();
    }


    public void Enqueue(AStarScoresTile tile)
    {
        _openTilesPriorityQueue.Enqueue(tile, (tile.FScore, tile.HScore));

        _updatedTileGScores[tile.Index] = (tile.GScore, tile); //💬 Adds new dictionary entry (OR updates it if already exists)
    }



    public AStarScoresTile Dequeue()
    {
        while (_openTilesPriorityQueue.Count > 0)
        {
            //💬 Get the tile at the front of the queue
            AStarScoresTile tileFromQueue = _openTilesPriorityQueue.Peek();

            //💬 Check if this tile's GScore matches the latest GScore in the dictionary
            if (_updatedTileGScores.TryGetValue(tileFromQueue.Index, out var latestTileInfo)  &&  (tileFromQueue.GScore == latestTileInfo.gScore))
            {
                //💬 This is the most up-to-date tile, so dequeue and return it
                _updatedTileGScores.Remove(tileFromQueue.Index);
                return _openTilesPriorityQueue.Dequeue();
            }

            //💬 If not up-to-date, just dequeue the outdated tile and continue the loop
            // (This is an old copy that was left lying around when the GScore was updated,
            // since unfortunately PriorityQueue has no Update() nor Remove() method.)
            _openTilesPriorityQueue.Dequeue();
        }
        //💬 if dictionary's Count == 0 :
        throw new InvalidOperationException("Queue is empty");
    }



    public bool Contains(Vector2Int index)
    {
        return _updatedTileGScores.ContainsKey(index);
    }



    public decimal GetGScore(Vector2Int index)
    {
        (decimal, AStarScoresTile) result = _updatedTileGScores[index];
        return result.Item1;
    }


    ///-------------------------------------------------------------------------------<summary>
    /// Used to update the GScore with a new LOWER value. Returns TRUE for success, 
    /// and FALSE if no tile was found at the provided index, or if provided GScore 
    /// was larger than the previous (resulting in NO CHANGE). </summary>
    public bool SetGScoreToNewLowerValue(Vector2Int index, decimal newGScore) //---------------------------
    {
        if (_updatedTileGScores.TryGetValue(index, out var oldTileInfo))
        {
            //💬 Create new updated tile, copying all fields from the old tile except for the new GScore:
            var updatedTile = new AStarScoresTile
            {
                HScore = oldTileInfo.tile.HScore,
                GScore = newGScore, //💬 <- INSERT NEW G SCORE VALUE
                FScore = oldTileInfo.tile.FScore,
                ParentTile = oldTileInfo.tile.ParentTile,
                Index = oldTileInfo.tile.Index,
            };

            _openTilesPriorityQueue.Enqueue(updatedTile, (updatedTile.FScore, updatedTile.HScore));
            _updatedTileGScores[index] = (newGScore, updatedTile);

            return true; //💬 SUCCESS
        }
        return false; //💬 If the tile isn't in the dictionary, it means it's not in the priority queue either
    }

}
