using UnityEngine;

/// <summary>
/// Encapsulates a rectangular area of our playing field in order to build and render fewer meshes (for performance purposes).
/// </summary>
public class DemoChunk
{
    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }

    public bool Dirty { get; set; }
    public GameObject GameObject { get; set; }
}