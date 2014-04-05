using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

/*
 * NOTE: most of the code here is solely for demo purposes... it isn't something you'd normally do in a real game.
 * Since we're changing the behavior of the FoW at run-time we have to update our unit states and can't let them be
 * quite as "automatic" as they normally would be.
 */

public class Demo : MonoBehaviour
{
    public GameObject ChunkPrefab;

    public GUITexture MinimapTexture;

    public GameObject[] TilePrefabs;
    public GameObject[] TilePrefabInstances;

    public GameObject PlayerUnitPrefab;
    public GameObject PlayerTowerPrefab;

    public GameObject EnemyUnitPrefab;
    public GameObject EnemyGhostPrefab;

    public GameObject EnemyTowerPrefab;
    public GameObject EnemyTowerGhostPrefab;

    public int SizeX = 128;
    public int SizeZ = 128;
    public int ChunkSize = 16;

    public float UpdateRate = 0.1f;

    private DemoChunk[,] _chunks;
    public DemoTile[,] Tiles;

    private GameObject _tilesParent;
    private GameObject _unitsParent;

    // note: we normally wouldn't use "List" (garbage collection, performance, etc.) but this is just a demo
    public readonly List<GameObject> Entities = new List<GameObject>();
    public readonly List<GameObject> PlayerUnits = new List<GameObject>();
    public readonly List<GameObject> EnemyUnits = new List<GameObject>();
    public readonly List<GameObject> PlayerTowers = new List<GameObject>();
    public readonly List<GameObject> EnemyTowers = new List<GameObject>();
    public readonly List<GameObject> UnitGhosts = new List<GameObject>();
    public readonly List<GameObject> TowerGhosts = new List<GameObject>();
    public readonly List<GameObject> Fires = new List<GameObject>();

    private FoWManager _fowManager;

    private bool _showMinimap = true;
    private bool _unitGhosts = true;
    private bool _towerGhosts = true;

    private int _textureRes = 1;
    private bool _texturePoint = false;
    private float _visibleLightMin = 0.8f;
    private float _exploredLight = 0.33f;
    private float _hiddenLight = 0.0f;
    private bool _fadeVis = true;

    private const int MaxPlayerUnits = 250;
    private const int MaxPlayerTowers = 250;
    private const int MaxEnemyUnits = 250;
    private const int MaxEnemyTowers = 250;

    private int _desiredPlayerUnitCount = 20;
    private int _desiredPlayerTowerCount = 20;
    private int _desiredEnemyUnitCount = 60;
    private int _desiredEnemyTowerCount = 60;

    private DemoMinimap _minimap;

    void Start()
    {
        _fowManager = FoWManager.FindInstance();
        _fowManager.Initialize();

        _minimap = (DemoMinimap)FindObjectOfType(typeof(DemoMinimap));

        //

        // Instantiate the prefabs so we can get the meshes
        // ... there must be a better way to do this, but I couldn't figure it out!
        TilePrefabInstances = new GameObject[TilePrefabs.Length];
        for (var i = 0; i < TilePrefabs.Length; i++)
        {
            TilePrefabInstances[i] = (GameObject)Instantiate(TilePrefabs[i], new Vector3(-1000, -1000, -1000), Quaternion.identity);
            // TilePrefabInstances[i].SetActive(false); <-- DO NOT DO THIS!!!
        }

        //

        _chunks = new DemoChunk[SizeX / ChunkSize, SizeZ / ChunkSize];

        Tiles = new DemoTile[SizeX, SizeZ];

        _tilesParent = new GameObject("Tiles");
        _unitsParent = new GameObject("Units");

        for (var x = 0; x < SizeX; x++)
        {
            for (var z = 0; z < SizeZ; z++)
            {
                RandomizeTile(x, z, true);
            }
        }

        UpdateDirtyChunks();

        //

        var callbacks = FoWCallbacks.FindInstance();

        if (callbacks != null)
        {
            callbacks.OnTileBecomesVisible = OnTileBecomesVisible;
            callbacks.OnTileBecomesExplored = OnTileBecomesExplored;
            callbacks.OnTileBecomesHidden = OnTileBecomesHidden;

            // Note: we're using FoWNonPlayerUnit.AutoManagerRenderers so we don't need this... it's here for reference
            // callbacks.OnNonPlayerUnitBecomesVisible = ShowEnemyUnit;
            // callbacks.OnNonPlayerUnitBecomesHidden = HideEnemyUnit;
            // callbacks.OnNonPlayerUnitBecomesExplored = HideEnemyUnit;

            callbacks.OnAddGhost = AddGhost;
            callbacks.OnRemoveGhost = RemoveGhost;
        }

        SetFoWValues();

        UpdatePlayerUnitCount();
        UpdatePlayerTowerCount();
        UpdateEnemyUnitCount();
        UpdateEnemyTowerCount();

    }

    private GameObject AddGhost(GameObject unit)
    {
        if (unit == null)
            return null;

        var pos = unit.transform.position;
        var rot = unit.transform.rotation;

        GameObject ghost;
        if (unit.GetComponent<DemoEnemy>() != null)
        {
            if (!_unitGhosts)
                return null;

            ghost = (GameObject)Instantiate(EnemyGhostPrefab, pos, rot);
            UnitGhosts.Add(ghost);
        }
        else
        {
            if (!_towerGhosts)
                return null;

            ghost = (GameObject)Instantiate(EnemyTowerGhostPrefab, pos, rot);
            TowerGhosts.Add(ghost);
        }

        Entities.Add(ghost);


        return ghost;
    }

    private void RemoveGhost(GameObject unit, GameObject ghost)
    {
        if (ghost == null)
            return;

        Entities.Remove(ghost);

        UnitGhosts.Remove(ghost);   // we don't know which it is, so we'll just remove both
        TowerGhosts.Remove(ghost);

        DestroyImmediate(ghost);
    }

    /*
    private void HideEnemyUnit(GameObject unit)
    {
        var enemyScr = unit.GetComponent<FoWNonPlayerUnit>();
        enemyScr.SetRender(false);
    }

    private void ShowEnemyUnit(GameObject unit)
    {
        var enemyScr = unit.GetComponent<FoWNonPlayerUnit>();
        enemyScr.SetRender(true);
    }
    */

    private void OnTileBecomesVisible(int tileX, int tileZ)
    {
        if (_minimap != null)
            _minimap.SetTile(tileX, tileZ, FoWTileState.Visible);

        //

        var tile = Tiles[tileX, tileZ];
        if (tile == null)
            return;

        if (tile.GhostType == tile.ActualType)
            return;

        tile.GhostType = tile.ActualType;
        var chunk = GetChunkForTile(tileX, tileZ);
        if (chunk != null)
            chunk.Dirty = true;
    }

    private void OnTileBecomesExplored(int tileX, int tileZ)
    {
        if (_minimap != null)
            _minimap.SetTile(tileX, tileZ, FoWTileState.Explored);

        //

        var tile = Tiles[tileX, tileZ];
        if (tile == null)
            return;

        if (tile.GhostType == tile.ActualType)
            return;

        tile.GhostType = tile.ActualType;
        var chunk = GetChunkForTile(tileX, tileZ);
        if (chunk != null)
            chunk.Dirty = true;
    }

    private void OnTileBecomesHidden(int tileX, int tileZ)
    {
        if (_minimap != null)
            _minimap.SetTile(tileX, tileZ, FoWTileState.Hidden);

        //
        // do nothing else for demo
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            int tileX, tileZ;
            if (GetTileFromCursor(out tileX, out tileZ))
            {

                var t = (int)Tiles[tileX, tileZ].GhostType;

                t++;
                if (t > (int)DemoTileType.Up)
                    t = (int)DemoTileType.Down;
                Tiles[tileX, tileZ].ActualType = (DemoTileType)t;
                if (TileIsVisible(tileX, tileZ))
                    Tiles[tileX, tileZ].GhostType = (DemoTileType)t;

                HandleTileChanged(tileX, tileZ);
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            if (_desiredPlayerUnitCount < MaxPlayerUnits)
            {
                int x, z;
                if (GetTileFromCursor(out x, out z))
                {
                    _desiredPlayerUnitCount++;
                    AddEntity(PlayerUnits, PlayerUnitPrefab, x, z);
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            if (_desiredPlayerTowerCount < MaxPlayerTowers)
            {
                int x, z;
                if (GetTileFromCursor(out x, out z))
                {
                    _desiredPlayerTowerCount++;
                    AddEntity(PlayerTowers, PlayerTowerPrefab, x, z);
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            if (_desiredEnemyUnitCount < MaxEnemyUnits)
            {
                int x, z;
                if (GetTileFromCursor(out x, out z))
                {
                    _desiredEnemyUnitCount++;
                    AddEntity(EnemyUnits, EnemyUnitPrefab, x, z);
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            if (_desiredEnemyTowerCount < MaxEnemyTowers)
            {
                int x, z;
                if (GetTileFromCursor(out x, out z))
                {
                    _desiredEnemyTowerCount++;
                    AddEntity(EnemyTowers, EnemyTowerPrefab, x, z);
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            int x, z;
            if (GetTileFromCursor(out x, out z))
            {
                RemoveAllEntitiesAtTile(x, z);
            }
        }
    }

    void LateUpdate()
    {
        UpdateDirtyChunks();
        UpdateUnitHeights();
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(16);

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        DemoGUIControls();
        GUILayout.Space(16);
        DemoInstructions();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DemoInstructions()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Camera: W,A,S,D + Z,X");

        GUILayout.Space(16);

        GUILayout.Label("KEYS:");
        GUILayout.Label("1 = Create Player Unit");
        GUILayout.Label("2 = Create Player Tower");
        GUILayout.Label("3 = Create Enemy Unit");
        GUILayout.Label("4 = Create Enemy Tower");
        GUILayout.Label("R = Remove Unit/Tower");
        GUILayout.Label("T = Change Terrain");

        GUILayout.Space(16);
        GUILayout.Label("Units:");
        GUILayout.Label("Blue = Player");
        GUILayout.Label("Red = Enemy");
        GUILayout.Label("Yellow = Enemy Ghost");

        GUILayout.EndVertical();
    }

    private void DemoGUIControls()
    {
        GUILayout.BeginVertical("box");

        if (MinimapTexture != null)
        {
            var origShowMinimap = _showMinimap;
            _showMinimap = GUILayout.Toggle(_showMinimap, "Show Minimap");
            if (origShowMinimap != _showMinimap)
            {
                MinimapTexture.enabled = _showMinimap;
            }
        }

        GUILayout.Space(8);

        var origUnitGhosts = _unitGhosts;
        _unitGhosts = GUILayout.Toggle(_unitGhosts, "Enemy Unit Ghosts");
        if (origUnitGhosts != _unitGhosts)
        {
            UpdateUnitGhosts();
        }

        var origTowerGhosts = _towerGhosts;
        _towerGhosts = GUILayout.Toggle(_towerGhosts, "Enemy Tower Ghosts");
        if (origTowerGhosts != _towerGhosts)
        {
            UpdateTowerGhosts();
        }

        GUILayout.Space(8);

        var origTextureRes = _textureRes;
        _textureRes = (int)Slider("Resolution", _textureRes, 1, 4);

        var origTexturePoint = _texturePoint;
        _texturePoint = GUILayout.Toggle(_texturePoint, "Point Clamp");

        GUILayout.Space(8);

        var origFadeVis = _fadeVis;
        _fadeVis = GUILayout.Toggle(_fadeVis, "Fade by Distance");

        var origVisibleLightMin = _visibleLightMin;
        if (_fadeVis)
        {
            _visibleLightMin = (float)Math.Round(Slider("Vis Min", _visibleLightMin, 0.5f, 1f), 2);
        }

        var origExploredLight = _exploredLight;
        _exploredLight = (float)Math.Round(Slider("Expl Light", _exploredLight, 0.25f, 0.5f), 2);

        var origHiddenLight = _hiddenLight;
        _hiddenLight = (float)Math.Round(Slider("Hidden Light", _hiddenLight, 0f, 0.25f), 2);

        if (origTextureRes != _textureRes
            || origTexturePoint != _texturePoint
            || origFadeVis != _fadeVis
            || origVisibleLightMin != _visibleLightMin
            || origExploredLight != _exploredLight
            || origHiddenLight != _hiddenLight
            )
        {
            SetFoWValues();
        }

        GUILayout.Space(8);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Explore All"))
        {
            for (var x = 0; x < SizeX; x++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    _fowManager.SetTileExplored(x, z, true);

                    var tile = Tiles[x, z];
                    if (tile != null)
                    {
                        tile.GhostType = tile.ActualType;
                    }
                }
            }

            MarkAllChunksDirty();

            _fowManager.RefreshTexture();
        }

        if (GUILayout.Button("Hide All"))
        {
            for (var x = 0; x < SizeX; x++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    _fowManager.SetTileExplored(x, z, false);
                }
            }

            _fowManager.RefreshTexture();
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Randomize"))
        {
            for (var x = 0; x < SizeX; x++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    RandomizeTile(x, z);
                }
            }

            MarkAllChunksDirty();
        }

        if (GUILayout.Button("Flatten"))
        {
            for (var x = 0; x < SizeX; x++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    Tiles[x, z].ActualType = DemoTileType.Flat;
                    if (TileIsVisible(x, z))
                        Tiles[x, z].GhostType = DemoTileType.Flat;
                }
            }

            MarkAllChunksDirty();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        _desiredPlayerUnitCount = (int)Slider("Player Units", _desiredPlayerUnitCount, 0, MaxPlayerUnits);
        UpdatePlayerUnitCount();

        _desiredPlayerTowerCount = (int)Slider("Player Towers", _desiredPlayerTowerCount, 0, MaxPlayerTowers);
        UpdatePlayerTowerCount();

        _desiredEnemyUnitCount = (int)Slider("Enemy Units", _desiredEnemyUnitCount, 0, MaxEnemyUnits);
        UpdateEnemyUnitCount();

        _desiredEnemyTowerCount = (int)Slider("Enemy Towers", _desiredEnemyTowerCount, 0, MaxEnemyTowers);
        UpdateEnemyTowerCount();

        GUILayout.Space(8);

        if (GUILayout.Button("Destroy All Ghosts"))
        {
            DestroyAllGhosts();
        }

        GUILayout.EndVertical();
    }

    private void UpdateUnitGhosts()
    {
        if (!_unitGhosts)
        {
            foreach (var ghost in UnitGhosts)
            {
                Entities.Remove(ghost);
                DestroyImmediate(ghost);
            }
            UnitGhosts.Clear();
        }

        foreach (var unit in EnemyUnits)
        {
            var scr = unit.GetComponent<FoWNonPlayerUnit>();
            if (scr != null)
                scr.CreateGhosts = _unitGhosts;
        }
    }

    private void UpdateTowerGhosts()
    {
        if (!_towerGhosts)
        {
            foreach (var ghost in TowerGhosts)
            {
                Entities.Remove(ghost);
                DestroyImmediate(ghost);
            }
            TowerGhosts.Clear();
        }

        foreach (var unit in EnemyTowers)
        {
            var scr = unit.GetComponent<FoWNonPlayerUnit>();
            if (scr != null)
                scr.CreateGhosts = _towerGhosts;
        }
    }

    private void UpdateEnemyTowerCount()
    {
        if (EnemyTowers.Count == _desiredEnemyTowerCount)
            return;

        var diff = _desiredEnemyTowerCount - EnemyTowers.Count;
        if (diff > 0)
            AddEntities(EnemyTowers, EnemyTowerPrefab, diff);
        else
            RemoveEntities(EnemyTowers, diff * -1);
    }

    private void UpdateEnemyUnitCount()
    {
        if (EnemyUnits.Count == _desiredEnemyUnitCount)
            return;

        var diff = _desiredEnemyUnitCount - EnemyUnits.Count;
        if (diff > 0)
            AddEntities(EnemyUnits, EnemyUnitPrefab, diff);
        else
            RemoveEntities(EnemyUnits, diff * -1);
    }

    private void UpdatePlayerTowerCount()
    {
        if (PlayerTowers.Count == _desiredPlayerTowerCount)
            return;

        var diff = _desiredPlayerTowerCount - PlayerTowers.Count;
        if (diff > 0)
            AddEntities(PlayerTowers, PlayerTowerPrefab, diff);
        else
            RemoveEntities(PlayerTowers, diff * -1);
    }

    private void UpdatePlayerUnitCount()
    {
        if (PlayerUnits.Count == _desiredPlayerUnitCount)
            return;

        var diff = _desiredPlayerUnitCount - PlayerUnits.Count;
        if (diff > 0)
            AddEntities(PlayerUnits, PlayerUnitPrefab, diff);
        else
            RemoveEntities(PlayerUnits, diff * -1);
    }

    private void RemoveEntities(List<GameObject> list, int count)
    {
        for (var i = 0; i < count; i++)
        {
            RemoveEntity(list);
        }
    }

    private void RemoveEntity(List<GameObject> list)
    {
        if (list.Count <= 0)
            return;

        var entity = list[0];
        Entities.Remove(entity);
        list.Remove(entity);
        DestroyImmediate(entity);
    }

    private void AddEntities(List<GameObject> list, GameObject prefab, int count)
    {
        for (var i = 0; i < count; i++)
        {
            int x, z;
            FindUnoccupiedTile(out x, out z);
            AddEntity(list, prefab, x, z);
        }
    }

    private void AddEntity(List<GameObject> list, GameObject prefab, int tileX, int tileZ)
    {
        RemoveAllEntitiesAtTile(tileX, tileZ);

        var h = GetHeightAt(tileX * 4, tileZ * 4);
        var pos = new Vector3(tileX * 4, h, tileZ * 4);
        var obj = (GameObject)Instantiate(prefab, pos, Quaternion.identity);
        obj.transform.parent = _unitsParent.transform;

        Entities.Add(obj);
        list.Add(obj);
    }

    private void FindUnoccupiedTile(out int x, out int z)
    {
        do
        {
            x = Random.Range(0, SizeX);
            z = Random.Range(0, SizeZ);
        } while (TileIsOccupied(x, z));
    }

    private void SetFoWValues()
    {
        _fowManager.TextureResolution = _textureRes;
        _fowManager.TexturePointFilter = _texturePoint;
        _fowManager.VisibleLightStrengthMin = _visibleLightMin;
        _fowManager.ExploredLightStrength = _exploredLight;
        _fowManager.HiddenLightStrength = _hiddenLight;
        _fowManager.FadeVisibility = _fadeVis;

        _fowManager.OnParameterChanges();
    }

    private float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ": (" + value.ToString("0.00") + ")");
        GUILayout.BeginVertical(GUILayout.Width(70));
        GUILayout.Space(10);
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(64));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        return value;
    }

    private void DestroyAllGhosts()
    {
        foreach (var ghost in UnitGhosts)
        {
            Entities.Remove(ghost);
            DestroyImmediate(ghost);
        }
        UnitGhosts.Clear();

        foreach (var ghost in TowerGhosts)
        {
            Entities.Remove(ghost);
            DestroyImmediate(ghost);
        }
        TowerGhosts.Clear();
    }

    public bool IsEnemyUnit(GameObject entity)
    {
        var scr = entity.GetComponent<DemoEnemy>();
        return scr != null;
    }

    public bool IsPlayerUnit(GameObject entity)
    {
        var scr = entity.GetComponent<DemoPlayer>();
        return scr != null;
    }

    public bool IsEnemyTower(GameObject entity)
    {
        var scr = entity.GetComponent<DemoEnemyTower>();
        return scr != null;
    }

    public bool IsPlayerTower(GameObject entity)
    {
        var scr = entity.GetComponent<DemoPlayerTower>();
        return scr != null;
    }

    public bool IsGhost(GameObject entity)
    {
        var scr = entity.GetComponent<FoWGhost>();
        return scr != null;
    }

    private void MarkAllChunksDirty()
    {
        for (var x = 0; x < SizeX / ChunkSize; x++)
        {
            for (var z = 0; z < SizeZ / ChunkSize; z++)
            {
                var chunk = _chunks[x, z];
                if (chunk == null)
                    continue;

                chunk.Dirty = true;
            }
        }
    }

    //
    //
    //

    private void RemoveAllEntitiesAtTile(int tileX, int tileZ)
    {
        RemoveAllEntitiesAtTile(PlayerUnits, tileX, tileZ);
        RemoveAllEntitiesAtTile(PlayerTowers, tileX, tileZ);
        RemoveAllEntitiesAtTile(EnemyUnits, tileX, tileZ);
        RemoveAllEntitiesAtTile(EnemyTowers, tileX, tileZ);
        RemoveAllEntitiesAtTile(UnitGhosts, tileX, tileZ);
        RemoveAllEntitiesAtTile(TowerGhosts, tileX, tileZ);
    }

    private readonly List<GameObject> _tempDestroyed = new List<GameObject>();
    private void RemoveAllEntitiesAtTile(ICollection<GameObject> list, int tileX, int tileZ)
    {
        _tempDestroyed.Clear();

        foreach (var entity in list)
        {
            int x, z;
            WorldPositionToTile(entity.transform.position, out x, out z);

            if (x == tileX && z == tileZ)
            {
                Entities.Remove(entity);
                _tempDestroyed.Add(entity);

                UpdateDesiredCount(entity);

                DestroyImmediate(entity);
            }
        }

        foreach (var entity in _tempDestroyed)
        {
            list.Remove(entity);
        }
    }

    private void UpdateDesiredCount(GameObject entity)
    {
        if (IsPlayerUnit(entity))
            _desiredPlayerUnitCount--;
        else if (IsPlayerTower(entity))
            _desiredPlayerTowerCount--;
        else if (IsEnemyUnit(entity))
            _desiredEnemyUnitCount--;
        else if (IsEnemyTower(entity))
            _desiredEnemyTowerCount--;
    }

    public void WorldPositionToTile(Vector3 position, out int tileX, out int tileZ)
    {
        tileX = (int)((position.x + 2) / 4);
        tileZ = (int)((position.z + 2) / 4);
    }

    public bool TileIsOccupied(int tileX, int tileZ)
    {
        foreach (var entity in Entities)
        {
            int x, z;
            WorldPositionToTile(entity.transform.position, out x, out z);

            if (x == tileX && z == tileZ)
                return true;
        }

        return false;
    }

    private void HandleTileChanged(int tileX, int tileZ)
    {
        var chunk = GetChunkForTile(tileX, tileZ);
        if (chunk != null)
            chunk.Dirty = true;
    }

    private bool GetTileFromCursor(out int tileX, out int tileZ)
    {
        tileX = 0;
        tileZ = 0;

        float x, z;

        if (GetTerrainPointFromCursor(out x, out z))
        {
            x += 2;
            z += 2;

            if (x < 0 || z < 0 || x > (SizeX * 4) || z > (SizeZ * 4))
            {
                return false;
            }

            tileX = (int)(x / 4);
            tileZ = (int)(z / 4);

            return true;
        }

        return false;
    }

    private bool GetTerrainPointFromCursor(out float x, out float z)
    {
        x = 0;
        z = 0;

        var plane = new Plane(Vector3.up, Vector3.zero);

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float distance;
        if (plane.Raycast(ray, out distance))
        {
            var hitPoint = ray.GetPoint(distance);

            x = hitPoint.x;
            z = hitPoint.z;

            if (x < 0 || z < 0 || x > (SizeX * 4) || z > (SizeZ * 4))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    //
    //
    //

    private void UpdateDirtyChunks()
    {
        // update chunks (if dirty)
        for (var chunkX = 0; chunkX < SizeX / ChunkSize; chunkX++)
        {
            for (var chunkZ = 0; chunkZ < SizeZ / ChunkSize; chunkZ++)
            {
                var chunk = _chunks[chunkX, chunkZ];
                if (chunk == null)
                    continue;

                if (!chunk.Dirty)
                    continue;

                var combines = new CombineInstance[ChunkSize * ChunkSize];

                var i = 0;
                for (var dx = 0; dx < ChunkSize; dx++)
                {
                    for (var dz = 0; dz < ChunkSize; dz++)
                    {
                        var x = dx + (chunkX * ChunkSize);
                        var z = dz + (chunkZ * ChunkSize);

                        var tile = Tiles[x, z];
                        if (tile != null)
                        {
                            var tileIndex = (int)tile.GhostType;
                            var prefab = TilePrefabInstances[tileIndex];
                            var filter = prefab.GetComponentInChildren<MeshFilter>();
                            if (filter == null)
                            {
                                Debug.LogError("NO MESH FILTER IN PREFAB!");
                            }
                            else
                            {
                                var tileMesh = filter.sharedMesh ?? filter.mesh;

                                var rot = prefab.transform.GetChild(0).rotation;
                                var scale = prefab.transform.GetChild(0).localScale;

                                combines[i].mesh = tileMesh;
                                combines[i].transform = Matrix4x4.TRS(new Vector3(dx * 4, 0, dz * 4), rot, scale);
                            }
                        }

                        i++;
                    }
                }

                var mesh = new Mesh();
                mesh.CombineMeshes(combines, true, true);
                chunk.GameObject.GetComponent<MeshFilter>().mesh = mesh;

                var chunkCollider = chunk.GameObject.GetComponent<MeshCollider>() ?? chunk.GameObject.AddComponent<MeshCollider>();

                if (chunkCollider == null)
                {
                    Debug.LogError("NO COLLIDER FOUND!");
                }
                else
                {
                    // chunkCollider.convex = true;
                    chunkCollider.sharedMesh = null;
                    chunkCollider.sharedMesh = mesh;
                }

                chunk.Dirty = false;
            }
        }
    }

    private void UpdateUnitHeights()
    {
        foreach (var entity in Entities)
        {
            if (entity == null)
                continue;

            var pos = entity.transform.position;
            pos.y = GetHeightAt(entity.transform.position.x, entity.transform.position.z);
            entity.transform.position = pos;
        }
    }

    public float GetHeightAt(float x, float z)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(new Vector3(x, 30, z), Vector3.down, out hitInfo, 40f, 1 << LayerMask.NameToLayer("Terrain")))
        {
            return hitInfo.point.y;
        }

        return 0;
    }

    private void RandomizeTile(int x, int z, bool initPlayerSees = false)
    {
        if (Tiles[x, z] == null)
        {
            Tiles[x, z] = new DemoTile();
        }

        var tileType = DemoTileType.Flat;

        var r = Random.Range(0, 10);
        if (r == 0)
            tileType = DemoTileType.Down;
        else if (r == 1)
            tileType = DemoTileType.Up;

        var chunk = GetChunkForTile(x, z);

        Tiles[x, z].ActualType = tileType;
        if (initPlayerSees || TileIsVisible(x, z))
            Tiles[x, z].GhostType = tileType;

        chunk.Dirty = true;
    }

    private bool TileIsVisible(int x, int z)
    {
        var fowtile = _fowManager.GetTile(x, z);
        if (fowtile == null)
            return true;

        return fowtile.IsVisible;
    }

    private DemoChunk GetChunkForTile(int x, int z)
    {
        var chunkX = x / ChunkSize;
        var chunkZ = z / ChunkSize;

        var chunk = _chunks[chunkX, chunkZ];
        if (chunk == null)
        {
            chunk = new DemoChunk() { ChunkX = chunkX, ChunkZ = chunkZ, Dirty = true };
            chunk.GameObject = (GameObject)Instantiate(ChunkPrefab, new Vector3(chunkX * ChunkSize * 4, 0, chunkZ * ChunkSize * 4), Quaternion.identity);
            chunk.GameObject.transform.parent = _tilesParent.transform;
            chunk.GameObject.name = "Chunk_" + chunkX + "_" + chunkZ;

            chunk.GameObject.layer = LayerMask.NameToLayer("Terrain");

            _chunks[chunkX, chunkZ] = chunk;
        }

        return chunk;
    }
}
