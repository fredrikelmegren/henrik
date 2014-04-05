using System;
using UnityEngine;

/// <summary>
/// Simple non-ordered memory-efficient collection of GameObjects.
/// Automatically grows as needed.   Never shrinks.
/// </summary>
public class GameObjectBag
{
    private readonly int _newPageSize;

    private GameObject[] _objects;

    private short _numObjects;

    public GameObjectBag(int initialSize = 256, int newPageSize = 256)
    {
        _newPageSize = newPageSize;
        _objects = new GameObject[initialSize];
        _numObjects = 0;
    }

    public void Resize(int newSize)
    {
        if (_objects.Length == newSize)
            return;

        if (newSize < _numObjects)
            newSize = _numObjects;

        Array.Resize(ref _objects, newSize);
    }

    public short Add(GameObject gobj)
    {
        if (_numObjects == _objects.Length)
        {
            Resize(_objects.Length + _newPageSize);
        }

        var index = _numObjects;
        _numObjects++;

        _objects[index] = gobj;

        return index;
    }

    public void RemoveAt(int index)
    {
        var lastIndex = _numObjects - 1;

        if (index == lastIndex)
        {
            // just nuke it
            _objects[index] = null;
        }
        else
        {
            // move the last object to take it's place
            _objects[index] = _objects[lastIndex];
        }

        _numObjects--;
    }

    public GameObject GetAt(int index)
    {
        return _objects[index];
    }

    public int Count { get { return _numObjects; } }


    public void Remove(GameObject viewer)
    {
        for (var i = 0; i < _numObjects; i++)
        {
            if (_objects[i] != viewer)
                continue;

            RemoveAt(i);
            return;
        }
    }
}