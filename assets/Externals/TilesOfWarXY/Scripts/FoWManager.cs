using System;
using UnityEngine;

/// <summary>
/// Manages our Fog of War data and targets (projector, image effect, or overlay plane), tracking player units, 
/// and providing callback services to the game.
/// </summary>
[Serializable]
public class FoWManager : MonoBehaviour
{
    #region INSPECTOR FIELDS

    public FoWType FogType = FoWType.Projector;

    public Vector3 WorldMin = new Vector3(0, 0, 0);         // minimum world coordinates that should be "fogged"
    public Vector3 WorldMax = new Vector3(256, 32, 256);    // maximum world coordinates that should be "fogged"
    public float WorldUnitsPerTileSide = 1f;                // number of Unity world units per tile

    public int TextureResolution = 1;                       // 1 - 4, pixels per tile (lower = less memory, higher = less blurry)
    public bool TexturePointFilter = false;                 // if not set, texture is Bilinear filtered

    // light values --> 0.0 = black, 1.0 = white
    public float VisibleLightStrengthMax = 1f;              // maximum light used in a "Visible" tile
    public float VisibleLightStrengthMin = 0.75f;           // minimum light used in a "Visible" tile (only used if FadeVisibility set)
    public float ExploredLightStrength = 0.5f;              // light value used in an "Explored" tile
    public float HiddenLightStrength = 0f;                  // light value used in a "Hidden" tile

    public bool FadeVisibility = true;                      // if set, lighting per tile will be based on distance from player units
    public bool ShowExplored = true;                        // if not set, no tiles will be made "Explored"

    public int InitialMaxViewers = 256;                     // initial size for GameObjectBag

    public Material ProMaterialReference;

    public enum FoWType
    {
        Projector = 0,
        ProShader = 1,
        Overlay = 2,
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region PRIVATE FIELDS

    [SerializeField, HideInInspector]
    private bool _isInitialized;        // we are sometimes called BEFORE our own Start method, this allows us to lazy initialize from any point

    [SerializeField, HideInInspector]
    private int _numTilesX;             // calculated play field size in tiles (width)

    [SerializeField, HideInInspector]
    private int _numTilesZ;             // calculated play field size in tiles (height)

    [SerializeField, HideInInspector]
    private Projector _projector;       // cache of our projector instance

    [SerializeField, HideInInspector]   // cache our plane
    private GameObject _plane;

    [SerializeField, HideInInspector]
    private Texture2D _fogTexture;    // the texture we're passing to projector

    [SerializeField, HideInInspector]
    private readonly Color32 _white = new Color32(255, 255, 255, 0);

    [SerializeField, HideInInspector]
    private readonly Color32 _black = new Color32(0, 0, 0, 0);

    [SerializeField, HideInInspector]
    private Color32 _colorHidden;       // calculated color (lighting) to use for hidden tiles

    [SerializeField, HideInInspector]
    private Color32 _colorExplored;     // calculated color (lighting) to use for explored tiles

    [SerializeField, HideInInspector]
    private Color32 _colorVisibleMin;   // calculated color (lighting) to use for visible tiles (min)

    [SerializeField, HideInInspector]
    private Color32 _colorVisibleMax;   // calculated color (lighting) to use for visible tiles (max)

    [SerializeField, HideInInspector]
    private FoWTileInfo[,] _tiles;      // our internal data about visibility of tiles

    [SerializeField, HideInInspector]
    private GameObjectBag _viewerBag;   // collection for all of our viewers

    // lazy evaluates our viewer collection creation
    private GameObjectBag Viewers
    {
        get { return _viewerBag ?? (_viewerBag = new GameObjectBag(InitialMaxViewers)); }
    }

    [SerializeField, HideInInspector]
    private bool _frameIsDirty = true;  // updated each frame to track whether anything notable changed since previous frame

    private FoWCallbacks _callbacks;    // cache of our callback instance to avoid multiple Finds

    private static FoWManager _instance;    // stored instance of this class (to avoid multiple "FindObject" calls)

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region UNITY STANDARD METHODS

    /// <summary>
    /// Standard Unity method.
    /// </summary>
    public void Reset()
    {
        FogType = FoWType.Projector;
        WorldMin = new Vector3(0, 0, 0);
        WorldMax = new Vector3(256, 32, 256);
        WorldUnitsPerTileSide = 1f;
        TextureResolution = 1;
        VisibleLightStrengthMax = 1f;
        VisibleLightStrengthMin = 0.75f;
        ExploredLightStrength = 0.5f;
        HiddenLightStrength = 0f;
        FadeVisibility = true;
        ShowExplored = true;
    }

    /// <summary>
    /// Standard Unity method.
    /// </summary>
    public void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Standard Unity method.  Updates all of our viewers and the projector texture if necessary.
    /// </summary>
    public void LateUpdate()
    {
        UpdateViewers(false);

        if (_frameIsDirty)
        {
            UpdateTexture();
            _frameIsDirty = false;
        }

        ResetViewers();
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region PUBLIC METHODS

    /// <summary>
    /// Convenience method to find the *single* instance of FoWManager in the game scene.
    /// </summary>
    /// <returns>Instance of FoWManager if found</returns>
    public static FoWManager FindInstance()
    {
        return _instance ?? (_instance = FindObjectOfType(typeof(FoWManager)) as FoWManager);
    }

    public Texture2D GetTexture()
    {
        Initialize();

        return _fogTexture;
    }

    /// <summary>
    /// Enables or disables fog of war projector.
    /// </summary>
    /// <param name="enableFoW">true to enable, false to disable</param>
    public void EnableFogOfWar(bool enableFoW)
    {
        _projector.enabled = enableFoW;
    }

    public void SetTileExplored(int tileX, int tileZ, bool explored)
    {
        var tile = GetTile(tileX, tileZ);
        if (tile != null)
        {
            if (!tile.IsVisible)
            {
                tile.IsExplored = explored;

                if (tile.IsExplored)
                    HandleTileBecomesExplored(tileX, tileZ);
                else
                    HandleTileBecomesHidden(tileX, tileZ);
            }
        }
    }

    public void AddViewer(GameObject viewer)
    {
        var index = Viewers.Add(viewer);
        var scr = viewer.GetComponent<FoWPlayerUnit>();
        if (scr != null)
        {
            scr.BagIndex = index;
        }
    }

    public void RemoveViewer(GameObject viewer)
    {
        var scr = viewer.GetComponent<FoWPlayerUnit>();

        // need to remove the "visible light radius" for this guy and mark any nearby viewers as needing update
        if (scr != null)
        {
            int currTileX, currTileZ;
            GetTileCoordinatesFromWorldPosition(viewer.transform.position, out currTileX, out currTileZ);

            SetExplored(scr.ViewRadiusInTiles, (short)currTileX, (short)currTileZ);
            SetRedrawFlagForNearby(scr.ViewRadiusInTiles, (short)currTileX, (short)currTileZ);
        }

        // now remove the viewer from our bag
        if (scr != null && scr.BagIndex >= 0 && scr.BagIndex < Viewers.Count)
        {
            // remove based on the index (fast)
            Viewers.RemoveAt(scr.BagIndex);
        }
        else
        {
            // we don't know the index, so try removing based on the actual reference (slow)
            Viewers.Remove(viewer);
        }

        _frameIsDirty = true;
    }

    public bool IsValidTile(int px, int pz)
    {
        return px >= 0 && px < _numTilesX && pz >= 0 && pz < _numTilesZ;
    }

    public FoWTileInfo GetTile(int tileX, int tileZ)
    {
        if (!IsValidTile(tileX, tileZ))
            return null;

        return _tiles[tileX, tileZ];
    }

    public void GetTileCoordinatesFromWorldPosition(Vector3 position, out int tileX, out int tileZ)
    {
        var xOffset = position.x - WorldMin.x;
        var zOffset = position.z - WorldMin.z;

        tileX = (int)(xOffset / WorldUnitsPerTileSide);
        tileZ = (int)(zOffset / WorldUnitsPerTileSide);
    }

    public FoWTileInfo GetTileFromWorldPosition(Vector3 position)
    {
        int tileX, tileZ;

        GetTileCoordinatesFromWorldPosition(position, out tileX, out tileZ);

        return GetTile(tileX, tileZ);
    }

    public void SetAllExplored(bool explored)
    {
        for (var x = 0; x < _numTilesX; x++)
        {
            for (var z = 0; z < _numTilesZ; z++)
            {
                if (!_tiles[x, z].IsVisible)
                    _tiles[x, z].IsExplored = explored;
            }
        }

        RefreshTexture();
    }

    public void RefreshTexture()
    {
        Initialize();

        for (var x = 0; x < _numTilesX; x++)
        {
            for (var z = 0; z < _numTilesZ; z++)
            {
                UpdateTile(x, z);
            }
        }

        _fogTexture.Apply();
    }

    public void OnParameterChanges()
    {
        Initialize();

        InitializeColors();
        InitializeTexture();

        UpdateViewers(true);

        RefreshTexture();
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region SETUP METHODS

    /// <summary>
    /// Initializes our instance if not already initialized
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        //

        _callbacks = GetComponent<FoWCallbacks>();

        InitializeSizes();

        InitializeTiles();

        InitializeColors();

        SetupFogTarget();

        InitializeTexture();

        //

        _isInitialized = true;
    }

    private void SetupFogTarget()
    {
        var projector = transform.FindChild("FoWProjector");
        if (projector != null && FogType == FoWType.Projector)
        {
            projector.gameObject.SetActive(true);

            _projector = transform.GetComponentInChildren<Projector>();
            if (_projector != null)
            {
                _projector.enabled = true;

                _projector.orthographic = true;
                _projector.orthoGraphicSize = (_numTilesZ * WorldUnitsPerTileSide * 0.5f);
                _projector.aspectRatio = _numTilesX / (float)_numTilesZ;

                _projector.farClipPlane = (WorldMax.y - WorldMin.y) + 10;

                transform.position = new Vector3(WorldMin.x + ((WorldMax.x - WorldMin.x) * 0.5f), WorldMax.y + 5,
                    WorldMin.z + ((WorldMax.z - WorldMin.z) * 0.5f));
            }
        }

        //

        var planeTransform = transform.FindChild("FoWOverlay");
        if (planeTransform != null)
        {
            planeTransform.position = new Vector3(WorldMin.x + ((WorldMax.x - WorldMin.x) * 0.5f), WorldMax.y, WorldMin.z + ((WorldMax.z - WorldMin.z) * 0.5f));
            planeTransform.localScale = new Vector3((WorldMax.x - WorldMin.x) * 0.1f, 1f, (WorldMax.z - WorldMin.z) * 0.1f);

            _plane = planeTransform.gameObject;

            if (FogType == FoWType.Overlay)
            {
                _plane.SetActive(true);
            }
        }

        //

        var fogProScripts = FindObjectsOfType(typeof(FoWPro));
        if (FogType == FoWType.ProShader)
        {
            if (fogProScripts.Length == 0)
            {
                // auto add the script
                var cam = Camera.mainCamera;
                if (cam != null)
                {
                    var fowp = cam.gameObject.AddComponent<FoWPro>();
                    fowp.Material = ProMaterialReference;
                }
            }
        }
        else
        {
            foreach (var scr in fogProScripts)
            {
                var fpscr = scr as FoWPro;
                if (fpscr != null)
                {
                    fpscr.enabled = false;
                }
            }
        }
    }

    private void InitializeSizes()
    {
        var worldSizeX = WorldMax.x - WorldMin.x;
        var worldSizeZ = WorldMax.z - WorldMin.z;
        _numTilesX = (int)(worldSizeX / WorldUnitsPerTileSide);
        _numTilesZ = (int)(worldSizeZ / WorldUnitsPerTileSide);
    }

    private void InitializeColors()
    {
        _colorHidden = Color32.Lerp(_black, _white, HiddenLightStrength);
        _colorExplored = Color32.Lerp(_black, _white, ExploredLightStrength);
        _colorVisibleMin = Color32.Lerp(_black, _white, VisibleLightStrengthMin);
        _colorVisibleMax = Color32.Lerp(_black, _white, VisibleLightStrengthMax);
    }

    private void InitializeTexture()
    {
        ClampTextureResolution();

        var textureSizeX = _numTilesX * TextureResolution;
        var textureSizeZ = _numTilesZ * TextureResolution;

        if (_fogTexture == null || _fogTexture.filterMode != (TexturePointFilter ? FilterMode.Point : FilterMode.Trilinear))
        {
            _fogTexture = new Texture2D(textureSizeX, textureSizeZ, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = TexturePointFilter ? FilterMode.Point : FilterMode.Trilinear
            };
        }
        else
        {
            if (_fogTexture.width != textureSizeX || _fogTexture.height != textureSizeZ)
            {
                _fogTexture.Resize(textureSizeX, textureSizeZ);
            }
        }

        for (var x = 0; x < textureSizeX; x++)
        {
            for (var z = 0; z < textureSizeZ; z++)
            {
                _fogTexture.SetPixel(x, z, _colorHidden);
            }
        }
        _fogTexture.Apply();

        SetFogTargetTexture();
    }

    private void SetFogTargetTexture()
    {
        //
        // Projector - set texture
        //

        if (_projector != null)
        {
            _projector.material.SetTexture("_ShadowTex", _fogTexture);
        }

        //
        // FoWPro shader - set texture
        //

        var shaders = FindObjectsOfType(typeof (FoWPro));
        foreach (var shader in shaders)
        {
            var fowProShader = shader as FoWPro;
            if (fowProShader != null)
            {
                fowProShader.SetValues(_fogTexture, WorldMin.x, WorldMin.z, WorldMax.x, WorldMax.z);
            }
        }

        //
        // Plan overlay - set texture
        //

        if (_plane != null)
        {
            _plane.renderer.material.SetTexture("_MainTex", _fogTexture);
        }
    }

    private void InitializeTiles()
    {
        _tiles = new FoWTileInfo[_numTilesX, _numTilesZ];
        for (var x = 0; x < _numTilesX; x++)
        {
            for (var z = 0; z < _numTilesZ; z++)
            {
                _tiles[x, z] = new FoWTileInfo();
            }
        }
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region PRIVATE METHODS

    /// <summary>
    /// Updates the projector texture for all changed units.
    /// </summary>
    private void UpdateTexture()
    {
        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers.GetAt(i);
            if (viewer == null)
                continue;

            var unit = viewer.GetComponent<FoWPlayerUnit>();
            if (unit == null)
            {
                Debug.LogWarning("Could not locate FoWPlayerUnit script on entity");
                continue;
            }

            //

            if (!unit.HasChanged)
                continue;

            var viewRadiusInTiles = unit.ViewRadiusInTiles;

            var tileX = unit.PreviousTileX;
            var tileZ = unit.PreviousTileZ;

            SetVisible(viewRadiusInTiles, tileX, tileZ);
        }

        _fogTexture.Apply();
    }

    /// <summary>
    /// Handles setting the area around a viewer (player unit) visible.
    /// Deals with tile visibility callbacks.
    /// </summary>
    /// <param name="viewRadiusInTiles">How far to set visibility</param>
    /// <param name="tileX">X Position of viewer</param>
    /// <param name="tileZ">Z Position of viewer</param>
    private void SetVisible(byte viewRadiusInTiles, short tileX, short tileZ)
    {
        if (tileX < 0 || tileZ < 0)
            return;

        var dist = viewRadiusInTiles * viewRadiusInTiles;

        for (var dx = -viewRadiusInTiles; dx <= viewRadiusInTiles; dx++)
        {
            for (var dz = -viewRadiusInTiles; dz <= viewRadiusInTiles; dz++)
            {
                var px = tileX + dx;
                var pz = tileZ + dz;

                if (!IsValidTile(px, pz))
                    continue;

                var v = new Vector2(dx, dz);
                var sqrmag = v.sqrMagnitude;
                var modsqrmag = sqrmag - (WorldUnitsPerTileSide * 0.5f);
                if (modsqrmag <= dist)
                {
                    if (TileIsVisible(tileX, tileZ, px, pz))
                    {
                        if (FadeVisibility)
                        {
                            var lightValue = Mathf.Lerp(0f, 1f, 1f - (modsqrmag / dist));

                            if (lightValue > _tiles[px, pz].SeenLightValue)
                            {
                                _tiles[px, pz].SeenLightValue = lightValue;
                            }
                        }
                        else
                        {
                            _tiles[px, pz].SeenLightValue = 1f;
                        }

                        var firstVisible = !_tiles[px, pz].IsExplored;

                        _tiles[px, pz].IsVisible = true;
                        _tiles[px, pz].IsExplored = true;

                        UpdateTile(px, pz);

                        if (firstVisible)
                            HandleTileFirstVisible(px, pz);
                        HandleTileBecomesVisible(px, pz);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles setting the area around a viewer (player unit) as explored.
    /// Deals with tile visibility callbacks.
    /// </summary>
    /// <param name="viewRadiusInTiles">How far to set visibility</param>
    /// <param name="unitTileX">X Position of viewer</param>
    /// <param name="unitTileZ">Z Position of viewer</param>
    private void SetExplored(byte viewRadiusInTiles, short unitTileX, short unitTileZ)
    {
        if (unitTileX < 0 || unitTileZ < 0)
            return;

        var dist = viewRadiusInTiles * viewRadiusInTiles;

        for (var dx = -viewRadiusInTiles; dx <= viewRadiusInTiles; dx++)
        {
            for (var dz = -viewRadiusInTiles; dz <= viewRadiusInTiles; dz++)
            {
                var px = unitTileX + dx;
                var pz = unitTileZ + dz;

                if (!IsValidTile(px, pz))
                    continue;

                var v = new Vector2(dx, dz);
                var sqrmag = v.sqrMagnitude;
                var modsqrmag = sqrmag - (WorldUnitsPerTileSide * 0.5f);
                if (modsqrmag <= dist)
                {
                    if (TileIsVisible(unitTileX, unitTileZ, px, pz))
                    {
                        _tiles[px, pz].IsVisible = false;
                        _tiles[px, pz].SeenLightValue = 0f;
                        _tiles[px, pz].IsExplored = true;

                        UpdateTile(px, pz);

                        HandleTileBecomesExplored(px, pz);
                    }
                }
            }
        }
    }

    private void UpdateViewers(bool force)
    {
        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers.GetAt(i);
            if (viewer == null)
                continue;

            var scr = viewer.GetComponent<FoWPlayerUnit>();
            if (scr == null)
            {
                Debug.LogWarning("Could not locate FoWPlayerUnit script on entity");
                continue;
            }

            //

            int currTileX, currTileZ;
            GetTileCoordinatesFromWorldPosition(viewer.transform.position, out currTileX, out currTileZ);

            if (force || currTileX != scr.PreviousTileX || currTileZ != scr.PreviousTileZ || scr.PreviousRadiusInTiles != scr.ViewRadiusInTiles)
            {
                _frameIsDirty = true;
                scr.HasChanged = true;

                SetExplored(scr.PreviousRadiusInTiles, scr.PreviousTileX, scr.PreviousTileZ);

                SetRedrawFlagForNearby(scr.ViewRadiusInTiles, scr.PreviousTileX, scr.PreviousTileZ);

                scr.PreviousTileX = (short)currTileX;
                scr.PreviousTileZ = (short)currTileZ;
                scr.PreviousRadiusInTiles = scr.ViewRadiusInTiles;
            }
        }
    }

    private void SetRedrawFlagForNearby(byte viewRadiusInTiles, short previousTileX, short previousTileZ)
    {
        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers.GetAt(i);
            if (viewer == null)
                continue;

            var scr = viewer.GetComponent<FoWPlayerUnit>();
            if (scr == null)
            {
                Debug.LogWarning("Could not locate FoWPlayerUnit script on entity");
                continue;
            }

            //

            if (Overlaps(previousTileX, previousTileZ, viewRadiusInTiles, scr.PreviousTileX, scr.PreviousTileZ,
                scr.ViewRadiusInTiles))
            {
                scr.HasChanged = true;
            }
        }
    }

    private bool Overlaps(short x1, short z1, byte r1, short x2, short z2, byte r2)
    {
        var d = r1 + r2;
        return (Math.Abs(x1 - x2) < d) && (Math.Abs(z1 - z2) < d);
    }

    private void ResetViewers()
    {
        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers.GetAt(i);
            if (viewer == null)
                continue;

            var scr = viewer.GetComponent<FoWPlayerUnit>();
            if (scr == null)
            {
                Debug.LogWarning("Could not locate FoWPlayerUnit script on entity");
                continue;
            }

            //

            scr.HasChanged = false;
        }
    }

    private bool TileIsVisible(int fromTileX, int fromTileZ, int toTileX, int toTileZ)
    {
        if (_callbacks != null && _callbacks.VisibilityTest != null)
            return _callbacks.VisibilityTest(fromTileX, fromTileZ, toTileX, toTileZ);

        return true;
    }

    private void UpdateTile(int x, int z)
    {
        if (!IsValidTile(x, z))
            return;

        var tile = _tiles[x, z];
        if (tile == null)
            return;

        var lightColor = _colorHidden;

        if (tile.IsVisible)
        {
            lightColor = Color32.Lerp(_colorVisibleMin, _colorVisibleMax, tile.SeenLightValue);
        }
        else if (ShowExplored && tile.IsExplored)
        {
            lightColor = _colorExplored;
        }
        // else lightColor = _colorHidden;

        SetTexturePixel(x, z, lightColor);
    }

    private void SetTexturePixel(int x, int z, Color32 lightColor)
    {
        ClampTextureResolution();

        for (var dx = 0; dx < TextureResolution; dx++)
        {
            var px = (x * TextureResolution);
            for (var dz = 0; dz < TextureResolution; dz++)
            {
                var pz = (z * TextureResolution);
                _fogTexture.SetPixel(px + dx, pz + dz, lightColor);
            }
        }
    }

    private void ClampTextureResolution()
    {
        if (TextureResolution < 1)
            TextureResolution = 1;
        else if (TextureResolution > 4)
            TextureResolution = 4;
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    #region CALLBACK UTILITY METHODS

    /// <summary>
    /// Callback utility method
    /// </summary>
    /// <param name="x">x position</param>
    /// <param name="z">z position</param>
    private void HandleTileBecomesVisible(int x, int z)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnTileBecomesVisible != null)
            _callbacks.OnTileBecomesVisible(x, z);
    }

    /// <summary>
    /// Callback utility method
    /// </summary>
    /// <param name="x">x position</param>
    /// <param name="z">z position</param>
    private void HandleTileFirstVisible(int x, int z)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnTileFirstVisible != null)
            _callbacks.OnTileFirstVisible(x, z);
    }

    /// <summary>
    /// Callback utility method
    /// </summary>
    /// <param name="x">x position</param>
    /// <param name="z">z position</param>
    private void HandleTileBecomesExplored(int x, int z)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnTileBecomesExplored != null)
            _callbacks.OnTileBecomesExplored(x, z);
    }

    /// <summary>
    /// Callback utility method
    /// </summary>
    /// <param name="x">x position</param>
    /// <param name="z">z position</param>
    private void HandleTileBecomesHidden(int x, int z)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnTileBecomesHidden != null)
            _callbacks.OnTileBecomesHidden(x, z);
    }

    public void HandleNonPlayerUnitBecomesVisible(GameObject unit)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnNonPlayerUnitBecomesVisible != null)
            _callbacks.OnNonPlayerUnitBecomesVisible(unit);
    }

    public void HandleNonPlayerUnitBecomesExplored(GameObject unit)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnNonPlayerUnitBecomesExplored != null)
            _callbacks.OnNonPlayerUnitBecomesExplored(unit);
    }

    public void HandleNonPlayerUnitBecomesHidden(GameObject unit)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnNonPlayerUnitBecomesHidden != null)
            _callbacks.OnNonPlayerUnitBecomesHidden(unit);
    }

    public void HandleRemoveGhost(GameObject unit, GameObject ghost)
    {
        if (_callbacks == null)
            return;

        if (_callbacks.OnRemoveGhost != null)
            _callbacks.OnRemoveGhost(unit, ghost);
    }

    public GameObject HandleAddGhost(GameObject unit)
    {
        if (_callbacks == null)
            return null;

        if (_callbacks.OnAddGhost != null)
            return _callbacks.OnAddGhost(unit);

        return null;
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////
}