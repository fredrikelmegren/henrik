using UnityEngine;
using System.Collections.Generic;

public class FogOfWarPlayer : MonoBehaviour
{
    List<Transform> wallTiles;
    List<Transform> floorTiles;

    Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
        
        wallTiles = new List<Transform>(GameObject.Find("Walls").GetComponentsInChildren<Transform>());
        floorTiles = new List<Transform>(GameObject.Find("Floor").GetComponentsInChildren<Transform>());
    }

    void Update()
    {
        
    }
}
