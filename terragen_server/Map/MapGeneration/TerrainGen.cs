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
    private readonly float _treesDensity;
    private float heightMountainMin = float.PositiveInfinity;
    private float heightMountainInvRange;
    private float heightForestMin = float.PositiveInfinity;
    private float heightForestInvRange;
    private float heightDesertMin = float.PositiveInfinity;
    private float heightDesertInvRange;
    private float tempMin = float.PositiveInfinity;
    private float heightTempInvRange;
    private float humidMin = float.PositiveInfinity;
    private float heightHumidInvRange;
    private Vector2 Forest   { get; } = new Vector2(0.5f, 0.8f);
    private Vector2 Mountains{ get; } = new Vector2(0.3f, 0.4f);
    private Vector2 Desert   { get; } = new Vector2(0.9f, 0.1f);
    private float wM;
    private float wF;
    private float wD;


    /// <summary>
    /// TerrainGen constructor
    /// </summary>
    /// <param name="seed">terrain global seed</param>
    /// <param name="myEncodedTree2D">FastNoise Encoded tree 2D</param>
    /// <param name="myEncodedTree3D">FastNoise Encoded tree 3D</param>
    /// <param name="caveDensity">cave density float</param>
    public TerrainGen(int seed, string myEncodedTree2D, string myEncodedTree3D, float caveDensity = 0.0f, float treesDensity = 0.04f)
    {
        _biomeGeneration = new BiomeGeneration(seed , myEncodedTree2D, myEncodedTree3D);
        _caveDensity = caveDensity;
        _treesDensity = treesDensity;
        
        SetGlobalMinMax(ref humidMin,ref heightHumidInvRange, 0.002f, "EwAfhes/BwA=");
        SetGlobalMinMax(ref tempMin, ref heightTempInvRange, 0.0015f, "DQAEAAAAAAAAQAcAAAAAAD8AAAAAAA==");
        SetGlobalMinMax(ref heightMountainMin, ref heightMountainInvRange, 0.005f, myEncodedTree2D);
        SetGlobalMinMax(ref heightForestMin, ref heightForestInvRange, 0.01f, "GQANAAIAAAAAAABAEwDNzMw+CAAAAAAAPwAAAAAAARkABAAAAAAAAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAETAOxROD4HAA==");
        SetGlobalMinMax(ref heightDesertMin, ref heightDesertInvRange, 0.01f, "GQANAAIAAAAAAABAEwCuR+E+BwAAAAAAPwAAAAAAARkABAAAAAAAAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAETAOxROD4HAA==");
    }

    /// <summary>
    /// Sets the global min and max values for terrain generation.
    /// </summary>
    public void SetMinMax()
    {
        var size = 1024;
        
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
        var rnd = new Random();
        float min = 0.0f, max = 8.0f;
        var valueInRange = rnd.NextSingle() * (max - min) + min;
        
        var chunk = new ChunkModel
        {
            ChunkData = new byte[65536] // 16 * 16 * 256 = 65536 bytes for a chunk
        };
        var heightMapMountain = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ);
        var treesMap = _biomeGeneration.GenerateNoise2DForTrees( ChunkSize, chunkX, chunkZ);
        var caveMap = _biomeGeneration.GenerateNoise3D( ChunkSize, ChunkHeight, chunkX, chunkZ);
        var heightMapForest = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ,  0.01f, "GQANAAIAAAAAAABAEwDNzMw+CAAAAAAAPwAAAAAAARkABAAAAAAAAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAETAOxROD4HAA==");
        var heightMapDesert = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ, 0.01f, "GQANAAIAAAAAAABAEwCuR+E+BwAAAAAAPwAAAAAAARkABAAAAAAAAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAETAOxROD4HAA==");
        var tempMap = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ, 0.0015f, "DQAEAAAAAAAAQAcAAAAAAD8AAAAAAA==");
        var humidMap = _biomeGeneration.GenerateNoise2D( ChunkSize, chunkX, chunkZ, 0.002f, "EwAfhes/BwA=");
        
        for (var x = 0; x < ChunkSize; ++x)
        {
            for (var z = 0; z < ChunkSize; ++z)
            {
                var temp  = GetNormalizeValue(ref tempMap [z * ChunkSize + x],ref tempMin,ref heightTempInvRange);
                var humid = GetNormalizeValue(ref humidMap[z * ChunkSize + x],ref humidMin,ref heightHumidInvRange);

                var hM = GetNormalizeValue(ref heightMapMountain[z * ChunkSize + x], ref heightMountainMin, ref heightMountainInvRange);
                var hF = GetNormalizeValue(ref heightMapForest  [z * ChunkSize + x], ref heightForestMin, ref heightForestInvRange);
                var hD = GetNormalizeValue(ref heightMapDesert  [z * ChunkSize + x], ref heightDesertMin, ref heightDesertInvRange);
                
                
                BiomeAdvantage(temp, humid, ref wM, ref wF, ref wD);
                var finalH = wM * hM + wF * hF + wD * hD;

                var h = (int)( finalH * (ChunkHeight - 1) );
                h = Math.Clamp(h, 0, ChunkHeight - 1);

                for (var y = 0; y < ChunkHeight; ++y)
                {
                    var index = (y * ChunkSize + z) * ChunkSize + x;
                    
                    if(y==0) chunk.ChunkData[index] = (byte)BlockType.BEDROCK;
                    else if ((y <= h) && (caveMap[index] < _caveDensity))
                    {
                        if(y>200&&y<=230) chunk.ChunkData[index] = (byte)BlockType.STONE;
                        else if(y>230) chunk.ChunkData[index] = (byte)BlockType.SNOW;
                        else if (y<=h-5) chunk.ChunkData[index] = (byte)BlockType.STONE;
                        else if (y<=h-1) chunk.ChunkData[index] = (byte)BlockType.DIRT;
                        else chunk.ChunkData[index] = (byte)BlockType.GRASS;
                    }
                    else if(chunk.ChunkData[index]!=(byte)BlockType.LEAVES) chunk.ChunkData[index] = (byte)BlockType.AIR;
                }
                
                var tree = treesMap[z * ChunkSize + x]*valueInRange;
                
                if (tree >= _treesDensity||x < 2 || x > 13 || z < 2 || z > 13) continue;

                if (h < 0 || h > 100 || caveMap[(h * ChunkSize + z) * ChunkSize + x]<_caveDensity) continue;

                /*generowanie pnia*/
                for (int dy = 1; dy <= 4; ++dy) {
                    int yy = h + dy;
                    int index = (yy * ChunkSize + z) * ChunkSize + x;
                    chunk.ChunkData[index] = (byte)BlockType.WOOD;
                }

                /*generowanie lisci*/
                for (int dx = -2; dx <= 2; ++dx) {
                    for (int dy = 3; dy <= 6; ++dy) {
                        for (int dz = -2; dz <= 2; ++dz) {

                            if (dx*dx + (dy-4)*(dy-4) + dz*dz > 8)
                                continue;

                            int xx = x + dx;
                            int yy = h + dy;
                            int zz = z + dz;

                            int idx = (yy * ChunkSize + zz) * ChunkSize + xx;

                            if (chunk.ChunkData[idx] == (byte)BlockType.AIR) {
                                chunk.ChunkData[idx] = (byte)BlockType.LEAVES;
                            }
                        }
                    }
                }
            }
        }

        return chunk;

    }
    
    public void SetGlobalMinMax(ref float Min, ref float invRange, float frequency, string code)
    {
        
        var Max = float.NegativeInfinity;
        for (int chunk_x = 0; chunk_x < 32; ++chunk_x) {
            for (int chunk_y = 0; chunk_y < 32; ++chunk_y) {
                var tempMap = _biomeGeneration.GenerateNoise2D( ChunkSize, chunk_x, chunk_y);

                for (int i = 0; i < ChunkSize * ChunkSize; ++i) {
                    Min = Math.Min(Min, tempMap[i]);
                    Max = Math.Max(Max, tempMap[i]);
                }
            }
        }
        Min = (float)(Min - (Max-Min) * 0.1);
        Max = (float)(Max + (Max-Min) * 0.1);
        invRange = (Max != Min) ? (1.0f / (Max - Min)) : 0.0f;
    }

    public float GetNormalizeValue(ref float value, ref float Min, ref float invRange)
    {
        float norm = (value - Min) * invRange;
        norm = Math.Clamp(norm, 0.0f, 1.0f);
        /*int h = static_cast<int>(normalized * (CHUNK_HEIGHT - 1));*/
        /*h = std::clamp(h, 0, CHUNK_HEIGHT - 1) - 100;*/
        return norm;
    }

    public void BiomeAdvantage(float temp, float humid, ref float wMountain, ref float wForest, ref float wDesert)
    {
        var mountain = Math.Sqrt(Math.Pow(temp-Mountains.X,2)+Math.Pow( humid-Mountains.Y,2))+ 1e-5f;
        var forest = Math.Sqrt(Math.Pow(temp-Forest.X,2)+Math.Pow(humid-Forest.Y,2))+ 1e-5f;
        var desert = Math.Sqrt(Math.Pow(temp-Desert.X,2)+Math.Pow(humid-Desert.Y,2))+ 1e-5f;
        var iM = 1/mountain;
        var iF = 1/forest;
        var iD = 1/desert;
        var sum = iM + iF + iD;
        wMountain = (float)(iM/sum);
        wForest = (float)(iF/sum);
        wDesert = (float)(iD/sum);
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
    STONE =3,
    WOOD =4,
    LEAVES =5,
    BEDROCK =6,
    SNOW =7
}