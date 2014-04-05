using System.Collections.Generic;
using UnityEngine;
using System.Collections;

// TODO:JT: enemy units are not clearing their previous paths

public class DemoMinimap : MonoBehaviour
{
    public GUITexture MinimapTexture;

    private Demo _demo;
    private Texture2D _texture;

    private const int Scale = 2;

    private FoWManager _fow;
    private Color32 _visibleColor;
    private Color32 _exploredColor;
    private Color32 _hiddenColor;
    private Color32 _playerUnitColor;
    private Color32 _enemyUnitColor;
    private Color32 _playerTowerColor;
    private Color32 _enemyTowerColor;
    private Color32 _enemyGhostColor;

    private class DemoUnitPosition
    {
        public int X { get; set; }
        public int Z { get; set; }

        public DemoUnitPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int Key { get { return GetKey(X, Z); } }

        public static int GetKey(int x, int z)
        {
            return (x * 10000) + z;
        }
    }

    private Dictionary<int, DemoUnitPosition> _prevUnitPositions = new Dictionary<int, DemoUnitPosition>();
    private Dictionary<int, DemoUnitPosition> _currUnitPositions = new Dictionary<int, DemoUnitPosition>();

    public void Start()
    {
        _demo = (Demo)FindObjectOfType(typeof(Demo));
        if (_demo == null)
            return;

        if (MinimapTexture == null)
            return;

        _fow = FoWManager.FindInstance();
        if (_fow == null)
            return;

        _texture = new Texture2D(_demo.SizeX * Scale, _demo.SizeZ * Scale, TextureFormat.ARGB32, false, false);
        ClearTexture();

        MinimapTexture.pixelInset = new Rect(-1 * _demo.SizeX * Scale, 0, _demo.SizeX * Scale, _demo.SizeZ * Scale);
        MinimapTexture.texture = _texture;

        UpdateMinimap();

        _visibleColor = new Color32(255, 255, 255, 255);
        _exploredColor = new Color32(64, 64, 64, 255);
        _hiddenColor = new Color32(0, 0, 0, 255);

        _playerUnitColor = new Color32(32, 32, 255, 255);
        _playerTowerColor = _playerUnitColor;

        _enemyUnitColor = new Color32(255, 32, 32, 255);
        _enemyTowerColor = _enemyUnitColor;

        _enemyGhostColor = new Color32(255, 200, 32, 255);
    }

    public void Update()
    {
        UpdateMinimap();
    }

    private void UpdateMinimap()
    {
        if (_fow == null)
            return;

        // update units and towers (tiles update via callbacks from FoWManager)
        AddUnitsAndTowers();

        _texture.Apply();
    }

    private void ClearTexture()
    {
        for (var x = 0; x < _texture.width; x++)
        {
            for (var z = 0; z < _texture.height; z++)
            {
                if (x == 0 || z == 0 || x == (_texture.width - 1) || z == (_texture.height - 1))
                    _texture.SetPixel(x, z, Color.blue);
                else
                    _texture.SetPixel(x, z, Color.black);
            }
        }
    }

    public void SetTile(int x, int z, FoWTileState tileState)
    {
        if (x == 0 || z == 0 || x == (_demo.SizeX - 1) || z == (_demo.SizeZ - 1))
        {
            // border!
            SetMinimapPixel(x, z, Color.blue);
            return;
        }

        switch (tileState)
        {
            case FoWTileState.Visible:
                SetMinimapPixel(x, z, _visibleColor);
                break;
            case FoWTileState.Explored:
                SetMinimapPixel(x, z, _exploredColor);
                break;
            case FoWTileState.Hidden:
                SetMinimapPixel(x, z, _hiddenColor);
                break;
        }
    }

    private void AddUnitsAndTowers()
    {
        var entities = _demo.Entities;

        for (var i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            if (e == null)
                continue;

            int x, z;

            _fow.GetTileCoordinatesFromWorldPosition(e.transform.position, out x, out z);

            var st = Remove(_prevUnitPositions, x, z) ?? new DemoUnitPosition(x, z);
            Add(_currUnitPositions, st);

            var fowtile = _fow.GetTile(x, z);
            if (fowtile == null)
                continue;

            if (_demo.IsPlayerUnit(e))
            {
                SetMinimapPixel(x, z, _playerUnitColor);
            }
            else if (_demo.IsPlayerTower(e))
            {
                SetMinimapPixel(x, z, _playerTowerColor);
            }
            else if (_demo.IsEnemyUnit(e))
            {
                if (fowtile.IsVisible)
                    SetMinimapPixel(x, z, _enemyUnitColor);
            }
            else if (_demo.IsEnemyTower(e))
            {
                if (fowtile.IsVisible)
                    SetMinimapPixel(x, z, _enemyTowerColor);
            }
            else if (_demo.IsGhost(e))
            {
                if (fowtile.IsVisible || fowtile.IsExplored)
                    SetMinimapPixel(x, z, _enemyGhostColor);
            }
        }

        // now go back and anything that is still in the unit list, "undraw it"
        foreach (var e in _prevUnitPositions.Values)
        {
            var fowtile = _fow.GetTile(e.X, e.Z);
            if (fowtile == null)
                continue;

            if (fowtile.IsHidden)
                SetMinimapPixel(e.X, e.Z, _hiddenColor);
            else if (fowtile.IsVisible)
                SetMinimapPixel(e.X, e.Z, _visibleColor);
            else if (fowtile.IsExplored)
                SetMinimapPixel(e.X, e.Z, _exploredColor);
        }

        // ... and then swap the list for next frame
        var temp = _prevUnitPositions;
        _prevUnitPositions = _currUnitPositions;
        _currUnitPositions = temp;

        _currUnitPositions.Clear();
    }

    private void Add(Dictionary<int, DemoUnitPosition> ht, int x, int z)
    {
        var key = DemoUnitPosition.GetKey(x, z);

        if (!ht.ContainsKey(key))
        {
            var st = new DemoUnitPosition(x, z);
            ht.Add(key, st);
        }
    }

    private void Add(Dictionary<int, DemoUnitPosition> ht, DemoUnitPosition st)
    {
        if (!ht.ContainsKey(st.Key))
        {
            ht.Add(st.Key, st);
        }
    }

    private DemoUnitPosition Remove(Dictionary<int, DemoUnitPosition> ht, int x, int z)
    {
        var key = DemoUnitPosition.GetKey(x, z);

        if (ht.ContainsKey(key))
        {
            var st = ht[key];
            ht.Remove(key);
            return st;
        }

        return null;
    }

    private void SetMinimapPixel(int x, int z, Color32 c)
    {
        for (var dx = 0; dx < Scale; dx++)
        {
            for (var dz = 0; dz < Scale; dz++)
            {
                var px = (x * Scale) + dx;
                var pz = (z * Scale) + dz;

                _texture.SetPixel(px, pz, c);
            }
        }
    }
}
