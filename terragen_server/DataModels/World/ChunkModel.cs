using ProtoBuf;

namespace terragen_server.DataModels.World;

/// <summary>
/// Chunk model
/// </summary>
public class ChunkModel
{
    public int X { get; set; }
    
    public int Z { get; set; }
    
    public byte[] ChunkData { get; set; } = new byte[65536];
    
    public byte Biome { get; set; }
}
