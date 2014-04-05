using UnityEngine;

/// <summary>
/// Should be attached to all "ghost" GameObjects.
/// This allows FoWManager to track when a "ghost" becomes visible.
/// </summary>
public class FoWGhost : MonoBehaviour
{
    private FoWManager _fow;

    [HideInInspector]
    public FoWTileState PreviousTileState = FoWTileState.Hidden;

    [HideInInspector]
    public GameObject GhostOfUnit;

    /// <summary>
    /// Standard Unity method.
    /// </summary>
    public void Start()
    {
        _fow = FoWManager.FindInstance();
    }

    /// <summary>
    /// Standard Unity method.
    /// </summary>
    public void LateUpdate()
    {
        // find our position
        var myTile = _fow.GetTileFromWorldPosition(transform.position);
        if (myTile == null)
            return;

        if (myTile.IsVisible || myTile.IsHidden)
        {
            // The player has "seen the ghost" and knows it doesn't exist any longer... so remove it
            // Note: We don't want to bother with rendering "hidden" ghosts so we'll remove them as well.
            //       This probably means the tile was set to "hidden" manually for some reason (clearing explored territory?)

            _fow.HandleRemoveGhost(GhostOfUnit, gameObject);
        }
    }
}