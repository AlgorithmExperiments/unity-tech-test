

using UnityEngine;


public struct AStarScoresTile
{

    ///-------------------------------------------------------------------------------<summary>
    /// HScore is a very rough approximation of the direct linear distance from the 
    /// current grid tile to the target destination tile, not accounting for any obstacles. 
    /// Also serves as a tie-breaker when deciding between two tiles with equal matching
    /// FScores - in such cases, the tile with the lower HScore receives preference. </summary>
    public decimal HScore; //------------------------------------------------------------------


    ///--------------------------------------------------------------------------<summary>
    /// GScore approximates the total non-linear travel from the starting grid tile
    /// to the current grid tile (to the intermediate point along the path). It is 
    /// a more accurate approximation than HScore, as it routes along the actual 
    /// path taken to the current tile, including deviations around obstacles. </summary>
    public decimal GScore; //-----------------------------------------------------------------


    ///--------------------------------------------------------------------------<summary>
    /// ⭐The composite score for the grid tile found by summing the GScore (distance from 
    /// starting point) and the HScore (distance to target destination). In general, 
    /// the A* algorithm maps the shortest route between two points by following the
    /// path through the neighboring Tiles that have the lowest FScores. </summary>
    public decimal FScore; //-----------------------------------------------------------------


    ///--------------------------------------------------------------------------<summary>
    /// Used to walk backward from the final destination to assemble the final
    /// result. Points to the index of the tile represents the shortest and most
    /// most direct route back to the starting tile. </summary>
    public Vector2Int ParentTile; //-----------------------------------------------------------------


    ///--------------------------------------------------------------------------<summary>
    /// The tile's index (x,y) in the NavGrid. </summary>
    public Vector2Int Index; //-----------------------------------------------------------



    public AStarScoresTile(decimal GScore, decimal HScore, decimal FScore, Vector2Int ParentTile, Vector2Int Index) {

        this.HScore = HScore;
        this.GScore = GScore;        
        this.FScore = FScore;
        this.ParentTile = ParentTile;
        this.Index = Index;

    }
}
