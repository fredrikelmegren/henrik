using UnityEngine;

/// <summary>
/// Should be attached to all "non-player unit" GameObjects.
/// Allows FoWManager to track the unit's position and destruction.
/// Allows FoWManager to track when the unit visibility changes for notifications and "ghosting".
/// </summary>
public class FoWNonPlayerUnit : MonoBehaviour
{
    [HideInInspector]
    public FoWTileState PreviousTileState = FoWTileState.Unknown;   // used to track tile state changes

    public bool CreateGhosts = false;           // determines whether "ghosts" should be created when unit moves from Visible to non-Visible

    public bool AutoManageRenderers = true;     // determines whether to enable/disable renderers when unit visibility changes
    public bool ShowInExplored = false;         // if Auto managing, determines whether unit is visible in Explored tiles
    public bool ShowInHidden = false;           // if Auto managing, determines whether unit is visible in Hidden tiles (not usually set)

    [HideInInspector]
    public GameObject Ghost;    // holds pointer to our "ghost" if we have created one for this unit

    private FoWManager _fow;

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
        if (_fow == null)
            return;

        // find our current tile
        var fowtile = _fow.GetTileFromWorldPosition(transform.position);
        if (fowtile == null)
            return;

        var currState = fowtile.TileState;

        // if our state didn't change, we don't need to do anything
        if (PreviousTileState != currState)
        {
            // our state changes, so handle it (possibly calling back to the game)
            switch (currState)
            {
                case FoWTileState.Visible:
                    if (AutoManageRenderers)
                    {
                        SetRender(true);
                    }
                    _fow.HandleNonPlayerUnitBecomesVisible(gameObject);
                    break;
                case FoWTileState.Explored:
                    if (AutoManageRenderers)
                    {
                        SetRender(ShowInExplored);
                    }
                    _fow.HandleNonPlayerUnitBecomesExplored(gameObject);
                    break;
                case FoWTileState.Hidden:
                    if (AutoManageRenderers)
                    {
                        SetRender(ShowInHidden);
                    }
                    _fow.HandleNonPlayerUnitBecomesHidden(gameObject);
                    break;
            }
        }

        //
        // Handle ghosts
        //

        HandleGhosts(currState);

        //
        // Capture our new state to use next frame
        //

        PreviousTileState = currState;
    }

    /// <summary>
    /// Automatically enables/disables all renderers under the game object
    /// </summary>
    /// <param name="shouldRender">true to enable renderers, false to disable</param>
    public void SetRender(bool shouldRender)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = shouldRender;
        }
    }

    /// <summary>
    /// Creates or removes ghosts as necessary.
    /// </summary>
    /// <param name="currTileState">The state of the tile containing this unit</param>
    private void HandleGhosts(FoWTileState currTileState)
    {
        if (!CreateGhosts)
            return; // we don't have to worry about ghosts

        var thisUnit = gameObject;

        if (currTileState == FoWTileState.Visible)
        {
            // the unit is visible, so hide it's ghost
            _fow.HandleRemoveGhost(thisUnit, Ghost);
            Ghost = null;
        }
        else if (currTileState == FoWTileState.Explored && PreviousTileState != FoWTileState.Explored && PreviousTileState != FoWTileState.Unknown)
        {
            // the unit isn't visible, so if it was visible LAST frame we'll add a ghost
            Ghost = _fow.HandleAddGhost(thisUnit);
            if (Ghost != null)
            {
                var ghostScr = Ghost.GetComponent<FoWGhost>();
                if (ghostScr != null)
                {
                    ghostScr.GhostOfUnit = thisUnit;    // keep track of our ghost
                }
            }
        }
    }
}
