using ProtoBuf;

namespace terragen_server.DataModels.World;

/// <summary>
/// Chunk model
/// </summary>
[ProtoContract]
public class ChunkModel
{
    [ProtoMember(1)]
    public int X { get; set; }
    
    [ProtoMember(2)]
    public int Z { get; set; }
    
    [ProtoMember(3)]
    public byte[] ChunkData { get; set; } = new byte[65536];
    
    [ProtoMember(4)]
    public byte Biome { get; set; }
}
