using System.Security.Cryptography;
using System.Text.Json;
using terragen_server.DataModels.World;
using terragen_server.FilesInput;
using terragen_server.Map.MapGeneration;
using terragen_server.Netcode;

namespace terragen_server.Map;

public class MapManager(int seed)
{
    private Dictionary<string, Region> _regions = new();

    private const string EncodeTree2D = "GQAHAAENAAQAAAAAACBABwAAZmYmPwAAAAA/";
    private const string EncodeTree3D = "EwCamZk+GgABEQACAAAAAADgQBAAAACIQR8AFgABAAAACwADAAAAAgAAAAMAAAAEAAAAAAAAAD8BFAD//wAAAAAAAD8AAAAAPwAAAAA/AAAAAD8BFwAAAIC/AACAPz0KF0BSuB5AEwAAAKBABgAAj8J1PACamZk+AAAAAAAA4XoUPw==";
    
    private TerrainGen? _terrainGen;
    
    /// <summary>
    /// Initializes the MapManager with the given seed.
    /// </summary>
    public void Init()
    {
        _terrainGen = new TerrainGen(seed, EncodeTree2D, EncodeTree3D);

        /*_terrainGen.SetMinMax();*/
        
        SrvLog.Console.Log("MAP_MANAGER", "MapManager initialized", SrvLog.LogTypes.INFO);
    }
    
    /// <summary>
    /// Gets a chunk from the map. If the chunk does not exist, it generates a new one.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>Chunk model data</returns>
    public ChunkModel GetChunk(int x, int z)
    {
        var regionX = (x < 0) ? (x / Region.GetRegionSize() - 1) : (x / Region.GetRegionSize());
        var regionZ = (z < 0) ? (z / Region.GetRegionSize() - 1) : (z / Region.GetRegionSize());
        
        var regionKey = $"{regionX}_{regionZ}";
        
        if (!_regions.ContainsKey(regionKey))
        {
            _regions[regionKey] = new Region(regionX, regionZ);
            SrvLog.Console.Log("MAP_MANAGER", $"Created region {regionKey}", SrvLog.LogTypes.DEBUG);
        }
        
        
        
        var region = _regions[regionKey];

        var localX = (x < 0) ? (x % Region.GetRegionSize() + Region.GetRegionSize() - 1) : (x % Region.GetRegionSize());
        var localZ = (z < 0) ? (z % Region.GetRegionSize() + Region.GetRegionSize() - 1) : (z % Region.GetRegionSize());
        
        //var localXz = x - regionX * Region.GetRegionSize();
        //var localZz = z - regionZ * Region.GetRegionSize();

        var chunk = region.LoadChunk(localX, localZ);
        
        //var chunk= region.LoadChunk(x, z);
        //var chunk= region.LoadChunk(x % Region.GetRegionSize(), z % Region.GetRegionSize());
        
        if (chunk == null)
        {
            SrvLog.Console.Log("MAP_MANAGER", $"new chunk region {regionKey}. Chunk: {x}, {z}", SrvLog.LogTypes.DEBUG);
            var newChunk = GenerateNewChunk(x, z);
            region.SaveChunk(localX, localZ, newChunk);
            return newChunk;
        }
        SrvLog.Console.Log("MAP_MANAGER", $"Loaded chunk {x} {z}", SrvLog.LogTypes.DEBUG);
        return chunk;
    }
    
    /// <summary>
    /// Loads a region into memory. If the region does not exist, it creates a new one.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    public void LoadRegion(int x, int z)
    {
        var regionKey = $"{x}_{z}";
        
        if (!_regions.ContainsKey(regionKey))
        {
            _regions[regionKey] = new Region(x, z);
            SrvLog.Console.Log("MAP_MANAGER", $"Created region {regionKey}", SrvLog.LogTypes.DEBUG);
        }
    }
    
    /// <summary>
    /// Loads a region into memory. If the region does not exist, it creates a new one.
    /// </summary>
    /// <param name="regionKey">region string key</param>
    public void LoadRegion(string regionKey)
    {
        if (!_regions.ContainsKey(regionKey))
        {
            var coords = regionKey.Split('_');
            var x = int.Parse(coords[0]);
            var z = int.Parse(coords[1]);
            
            _regions[regionKey] = new Region(x, z);
            SrvLog.Console.Log("MAP_MANAGER", $"Created region {regionKey}", SrvLog.LogTypes.DEBUG);
        }
    }

    /// <summary>
    /// Generates a new chunk
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>Chunk model data</returns>
    private ChunkModel GenerateNewChunk(int x, int z)
    {
        var chunkData = _terrainGen?.GenerateChunks(x, z);

        // ReSharper disable once NullableWarningSuppressionIsUsed
        return chunkData!;
    }
    
    /// <summary>
    /// Removes a region from memory. If the region is not loaded, it does nothing.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    public void RemoveRegion(int x, int z)
    {
        var regionKey = $"{x}_{z}";
        
        if (_regions.ContainsKey(regionKey))
        {
            _regions.Remove(regionKey);
            SrvLog.Console.Log("MAP_MANAGER", $"Removed region {regionKey}", SrvLog.LogTypes.DEBUG);
        }
    }
    
    /// <summary>
    /// Removes a region from memory. If the region is not loaded, it does nothing.
    /// </summary>
    /// <param name="regionKey">region key, format: X_Z</param>
    public void RemoveRegion(string regionKey)
    {
        if (_regions.ContainsKey(regionKey))
        {
            _regions.Remove(regionKey);
            SrvLog.Console.Log("MAP_MANAGER", $"Removed region {regionKey}", SrvLog.LogTypes.DEBUG);
        }
    }

    /// <summary>
    /// Updates a chunk in the map. If the chunk does not exist, it's do nothing.
    /// </summary>
    /// <param name="ChunkX">Chunk coordinate X</param>
    /// <param name="ChunkZ">Chunk coordinate Z</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="blockId">new block id</param>
    public void UpdateChunk(int ChunkX, int ChunkZ, int x, int y, int z, byte blockId)
    {
        var regionX = (ChunkX < 0) ? (ChunkX / Region.GetRegionSize() - 1) : (ChunkX / Region.GetRegionSize());
        var regionZ = (ChunkZ < 0) ? (ChunkZ / Region.GetRegionSize() - 1) : (ChunkZ / Region.GetRegionSize());
        
        var regionKey = $"{regionX}_{regionZ}";
        
        if (_regions.ContainsKey(regionKey))
        {
            var region = _regions[regionKey];
            
            var chunk= region.LoadChunk(ChunkX % Region.GetRegionSize(), ChunkZ % Region.GetRegionSize());
            
            var blockOffset = (y * 16 + x) * 16 + z;

            if (chunk != null) chunk.ChunkData[blockOffset - 1] = blockId;
            
            region.SaveChunk(ChunkX % Region.GetRegionSize(), ChunkZ % Region.GetRegionSize(), chunk);
        }
    }
}