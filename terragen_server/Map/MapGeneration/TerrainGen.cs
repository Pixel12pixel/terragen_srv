using System.Numerics;
using terragen_server.DataModels.World;

namespace terragen_server.Map.MapGeneration;

public class TerrainGen
{
    private const int ChunkSize = 16;
    private const int ChunkHeight = 256;
    private readonly BiomeGeneration _biomeGeneration;
    private float _invRange;
    private float _globalMin = float.PositiveInfinity;
    private readonly float _caveDensity;

    /// <summary>
    /// TerrainGen constructor
    /// </summary>
    /// <param name="seed">terrain global seed</param>
    /// <param name="myEncodedTree2D">FastNoise Encoded tree 2D</param>
    /// <param name="myEncodedTree3D">FastNoise Encoded tree 3D</param>
    /// <param name="caveDensity">cave density float</param>
    public TerrainGen(int seed, string myEncodedTree2D, string myEncodedTree3D, float caveDensity = 0.0f)
    {
        _biomeGeneration = new BiomeGeneration(seed , myEncodedTree2D, myEncodedTree3D);
        _caveDensity = caveDensity;
    }

    /// <summary>
    /// Sets the global min and max values for terrain generation.
    /// </summary>
    public void SetMinMax()
    {
        var size = 128;//TODO: 1024
        
        var globalMax = float.NegativeInfinity;
        
        for (var chunkX = 0; chunkX < size; ++chunkX)
        {
            for (var chunkY = 0; chunkY < size; ++chunkY)
            {
                //if (cts.IsCancellationRequested) return;
                var tempHeightMap = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkY);
                
                foreach (var value in tempHeightMap)
                {
                    _globalMin = Math.Min(_globalMin, value);
                    globalMax = Math.Max(globalMax, value);
                }
                
                var iteration = chunkX * size + chunkY;
                
                var progress = (float)iteration / (float)(size * size);
                if (iteration % (size * (Math.DivRem((int)size, (int)(size / 16), out _))) == 0)
                {
                    SrvLog.Console.Log("TERRAIN_GEN", $"Generating world: {progress:P2}", SrvLog.LogTypes.INFO);
                }
            }
        }
        
        _globalMin -= (globalMax - _globalMin) * 0.1f;
        globalMax += (globalMax - _globalMin) * 0.1f;
        
        _invRange = (globalMax != _globalMin) ? (1.0f / (globalMax - _globalMin)) : 0.0f;
        
        //readySignal.SetResult();
    }

    /// <summary>
    /// Generates a chunk of terrain based on the provided chunk coordinates.
    /// </summary>
    /// <param name="chunkX">X coordinate</param>
    /// <param name="chunkZ">Z coordinate</param>
    /// <returns>new chunk model</returns>
    public ChunkModel GenerateChunks(int chunkX, int chunkZ) 
    {

        var chunk = new ChunkModel
        {
            ChunkData = new byte[65536] // 16 * 16 * 256 = 65536 bytes for a chunk
        };
        var heightMap = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ);
        var caveMap = _biomeGeneration.GenerateNoise3D( ChunkSize, ChunkHeight, chunkX, chunkZ);
        
        for (var x = 0; x < ChunkSize; ++x)
        {
            for (var z = 0; z < ChunkSize; ++z)
            {
                var v = heightMap[z * ChunkSize + x];
                var normalized = (v - _globalMin) * _invRange;

                var h = (int)(normalized * (ChunkHeight - 1));
                h = Math.Clamp(h, 0, ChunkHeight - 1) - 100;

                for (var y = 0; y < ChunkHeight; ++y)
                {
                    var index = (y * ChunkSize + z) * ChunkSize + x;
                    if ((y <= h) && (caveMap[index] < _caveDensity))
                        chunk.ChunkData[index] = (byte)BlockType.DIRT;
                    else
                        chunk.ChunkData[index] = (byte)BlockType.AIR;
                }
            }
        }

        return chunk;

    }
    
}

/// <summary>
/// BlockType enum representing different types of blocks in the terrain.
/// </summary>
public enum BlockType : byte
{
    AIR =0,
    GRASS =1,
    DIRT =2,
    STONE =3
}