
using UnityEngine;

public struct NavGridTile
{
    ///--------------------------------------------------------------------------<summary>
    /// Indicates that the grid tile is free and clear of any pathing obstructions that 
    /// would prohibit path navigation through the grid Tile. </summary>
    public bool IsTraversable; //---------------------------------------------------------

    ///--------------------------------------------------------------------------<summary>
    /// The tile's index (x,y) in the NavGrid. </summary>
    public Vector2Int Index; //-----------------------------------------------------------

    ///--------------------------------------------------------------------------<summary>
    /// The world position of the tile's centerpoint (x,y,z). </summary>
    public Vector3 CenterPointWorldPosition; //------------------------------------------------------


    public NavGridTile(bool IsTraversable, Vector2Int Index, Vector3 CenterPointWorldPosition) {

        this.IsTraversable = IsTraversable;
        this.Index = Index;
        this.CenterPointWorldPosition = CenterPointWorldPosition;

    }
}
