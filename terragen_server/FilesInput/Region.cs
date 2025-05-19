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
        Directory.CreateDirectory(folder);
        
        _filePath = Path.Combine(folder, $"region_{regionX}_{regionZ}.lethal"); //TODO: .trg
        _chunkIndex = new();

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
        using var fs = new FileStream(_filePath, FileMode.Open);
        using var br = new BinaryReader(fs);
        
        for (var i = 0; i < ChucksPerRegion; i++)
        {
            var offset = br.ReadInt32();
            var length = br.ReadInt32();
            
            if (length > 0)
            {
                _chunkIndex.Add((i % RegionSize, i / RegionSize), (offset, length));
            }
        }
    }
    
    /// <summary>
    /// Saves a chunk to the region file.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="data">chunk data</param>
    public void SaveChunk(int x, int z, byte[] data)
    {
        var index = GetChunkIndex(x, z);
        var offset = HeaderSize + index * data.Length;
        
        using var fs = new FileStream(_filePath, FileMode.Open);
        using var bw = new BinaryWriter(fs);
        
        fs.Seek(index * HeaderEntrySize, SeekOrigin.Begin);
        bw.Write(offset);
        bw.Write(data.Length);
        
        fs.Seek(offset, SeekOrigin.Begin);
        bw.Write(data);
        
        _chunkIndex[(x, z)] = (offset, data.Length);
    }

    /// <summary>
    /// Loads a chunk from the region file.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>chunk data, null if not exist</returns>
    public byte[]? LoadChunk(int x, int z)
    {
        var index = GetChunkIndex(x, z);
        
        if (_chunkIndex.TryGetValue((x, z), out var entry))
        {
            using var fs = new FileStream(_filePath, FileMode.Open);
            using var br = new BinaryReader(fs);
            
            fs.Seek(entry.offset, SeekOrigin.Begin);
            return br.ReadBytes(entry.length);
        }

        return null;
    }

    /// <summary>
    /// Disposes the region object and clears the chunk index.
    /// </summary>
    public void Dispose()
    {
        _chunkIndex.Clear();
    }
}