using ProtoBuf;
using terragen_server.DataModels.World;

namespace terragen_server.FilesInput;

public class Region() : IDisposable
{
    private const int RegionSize = 16;
    private const int ChucksPerRegion = RegionSize * RegionSize;
    private const int HeaderEntrySize = 4 * 2;
    private const int HeaderSize = HeaderEntrySize * ChucksPerRegion;
    
    private readonly string _filePath;
    private readonly Dictionary<(int, int), (int offset, int length)> _chunkIndex;

    /// <summary>
    /// Gets the size of the region.
    /// </summary>
    /// <returns>region size</returns>
    public static int GetRegionSize()
    {
        return RegionSize;
    }
    
    /// <summary>
    /// Region constructor.
    /// </summary>
    /// <param name="regionX">X coordinate</param>
    /// <param name="regionZ">Z coordinate</param>
    /// <param name="folder">folder name</param>
    public Region(int regionX, int regionZ, string folder = "regions") : this()
    {
        var folderPath = "/app/data/";
        
        //folder = Path.Combine(folderPath, folder); //linux
        
        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, $"region_{regionX}_{regionZ}.trg");
        _chunkIndex = new Dictionary<(int, int), (int offset, int length)>();

        if (File.Exists(_filePath))
        {
            LoadHeader();
        }
        else
        {
            using var fs = new FileStream(_filePath, FileMode.Create);
            fs.SetLength(HeaderSize);
        }
    }
    
    /// <summary>
    /// Gets the chunk index based on the x and z coordinates.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>normalized chunk index</returns>
    private int GetChunkIndex(int x, int z)
    {
        return (Math.Abs(x) % RegionSize) + (Math.Abs(z) % RegionSize) * RegionSize;
    }
    
    /// <summary>
    /// Loads the header from the region file.
    /// </summary>
    private void LoadHeader()
    {
        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        
        for (var i = 0; i < ChucksPerRegion; i++)
        {
            var offset = br.ReadInt32();
            var length = br.ReadInt32();
            
            if (length > 0)
                _chunkIndex[(i % RegionSize, i / RegionSize)] = (offset, length);
            /*
            if (length > 0)
            {
                _chunkIndex.Add((i % RegionSize, i / RegionSize), (offset, length));
            }*/
        }
    }
    
    /// <summary>
    /// Saves a chunk to the region file.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="chunk">chunk</param>
    public void SaveChunk(int x, int z, ChunkModel chunk)
    {
        var index = GetChunkIndex(x, z);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, chunk);
        var data = ms.ToArray();

        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        fs.Seek(0, SeekOrigin.End); 
        var offset = (int)fs.Position;
        fs.Write(data);
        
        fs.Seek(index * HeaderEntrySize, SeekOrigin.Begin);
        using var bw = new BinaryWriter(fs);
        bw.Write(offset);
        bw.Write(data.Length);
        
        fs.Close();
        bw.Close();
        
        try
        {
            _chunkIndex.Add((x, z), (offset, data.Length));
        }
        catch (Exception e)
        {
            SrvLog.Console.Log($"Failed to add chunk index: {e.Message}", SrvLog.LogTypes.WARNING);
        }
        
    }
    
    /// <summary>
    /// Loads a chunk from the region file.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>chunk data, null if not exist</returns>
    public ChunkModel? LoadChunk(int x, int z)
    {
        if (!_chunkIndex.TryGetValue((x, z), out var entry))
            return null;

        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        fs.Seek(entry.offset, SeekOrigin.Begin);
        var data = new byte[entry.length];
        fs.ReadExactly(data);

        using var ms = new MemoryStream(data);
        var a = Serializer.Deserialize<ChunkModel>(ms);
        return a;
    }
    
    /// <summary>
    /// Disposes the region object and clears the chunk index.
    /// </summary>
    public void Dispose()
    {
        _chunkIndex.Clear();
    }
}