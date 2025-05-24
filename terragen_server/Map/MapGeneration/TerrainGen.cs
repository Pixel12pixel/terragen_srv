using System.Numerics;
using terragen_server.DataModels.World;

namespace terragen_server.Map.MapGeneration;

public class TerrainGen
{
    private const int CHUNK_SIZE = 16;
    private const int CHUNK_HEIGHT = 256;
    private BiomeGeneration biomeGeneration;
    private float invRange;
    float globalMin = float.PositiveInfinity;
    private readonly float caveDensity;

    public TerrainGen(int seed, string myEncodedTree2D, string myEncodedTree3D, float caveDensity = 0.0f)
    {
        biomeGeneration = new BiomeGeneration(seed , myEncodedTree2D, myEncodedTree3D);
        this.caveDensity = caveDensity;
    }

    public async Task SetMinAndMax(TaskCompletionSource readySignal, CancellationTokenSource cts)
    {
        
        float globalMax = float.NegativeInfinity;
        
        for (var chunkX = 0; chunkX < 32; ++chunkX)
        {
            for (var chunkY = 0; chunkY < 32; ++chunkY)
            {
                if (cts.IsCancellationRequested) return;
                var tempHeightMap = biomeGeneration.GenerateNoise2D( CHUNK_SIZE, chunkX, chunkY);
                
                foreach (var value in tempHeightMap)
                {
                    globalMin = Math.Min(globalMin, value);
                    globalMax = Math.Max(globalMax, value);
                }
            }
        }
        
        globalMin -= (globalMax - globalMin) * 0.1f;
        globalMax += (globalMax - globalMin) * 0.1f;
        
        invRange = (globalMax != globalMin) ? (1.0f / (globalMax - globalMin)) : 0.0f;
        
        readySignal.SetResult();
    }

    public ChunkModel GenerateChunks(int chunkX, int chunkZ) 
    {

        ChunkModel chunk = new ChunkModel();
        var heightMap = biomeGeneration.GenerateNoise2D( CHUNK_SIZE, chunkX, chunkZ);
        var caveMap = biomeGeneration.GenerateNoise3D( CHUNK_SIZE, CHUNK_HEIGHT, chunkX, chunkZ);
        
        for (var x = 0; x < CHUNK_SIZE; ++x)
        {
            for (var z = 0; z < CHUNK_SIZE; ++z)
            {
                var v = heightMap[z * CHUNK_SIZE + x];
                var normalized = (v - globalMin) * invRange;

                var h = (int)(normalized * (CHUNK_HEIGHT - 1));
                h = Math.Clamp(h, 0, CHUNK_HEIGHT - 1) - 100;

                for (var y = 0; y < CHUNK_HEIGHT; ++y)
                {
                    var index = (y * CHUNK_SIZE + z) * CHUNK_SIZE + x;
                    if ((y <= h) && (caveMap[index] < caveDensity))
                        chunk.ChunkData[index] = (byte)BlockType.DIRT;
                    else
                        chunk.ChunkData[index] = (byte)BlockType.AIR;
                }
            }
        }

        return chunk;

    }
    
}

public enum BlockType : byte
{
    AIR =0,
    GRASS =1,
    DIRT =2,
    STONE =3
}