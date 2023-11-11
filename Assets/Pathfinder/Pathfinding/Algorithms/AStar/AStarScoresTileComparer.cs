using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AStarScoresTileComparer : IComparer<(decimal, decimal)>
{

    ///---------------------------------------------------------------------------<summary>
    /// Compares two AStarScoresTiles by analyzing their <FScore, HScore> tuple. </summary>
    public int Compare((decimal, decimal) tileA, (decimal, decimal) tileB) //--------------
    {
        //💬 Item1 = FScore, Item2 = HScore
        if (tileA.Item1 != tileB.Item1)
        {
            //💬 return the tile with the lower F-score
            return tileA.Item1.CompareTo(tileB.Item1);
        }
        //💬 else if F-scores are equal, use prefer the tile with the lower H-score:
        return tileA.Item2.CompareTo(tileB.Item2);
    }

}
