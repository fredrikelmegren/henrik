/// <summary>
/// Encapsulates the information we track for a tile.
/// </summary>
public class FoWTileInfo
{
    public FoWTileInfo()
    {
        SeenLightValue = 0f;
    }

    public float SeenLightValue = 0f;   // the lighting value for this tile, lower is darker

    public bool IsVisible { get; set; }
    public bool IsExplored { get; set; }
    public bool IsHidden { get { return !IsExplored && !IsVisible; } }

    public FoWTileState TileState
    {
        get
        {
            if (IsVisible)
                return FoWTileState.Visible;
            return IsExplored ? FoWTileState.Explored : FoWTileState.Hidden;
        }
    }
}
