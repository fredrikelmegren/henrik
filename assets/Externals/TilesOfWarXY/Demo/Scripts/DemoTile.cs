using UnityEngine;

/// <summary>
/// Encapsulates the state of our demo tiles.   We track what the actual tile type should be as well as what the player thinks is there (ghost).
/// </summary>
public class DemoTile
{
    public DemoTileType ActualType { get; set; }
    public DemoTileType GhostType { get; set; }
}