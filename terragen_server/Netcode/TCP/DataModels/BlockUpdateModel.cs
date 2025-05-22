using ProtoBuf;

namespace terragen_server.Netcode.TCP.DataModels;

[ProtoContract]
public class BlockUpdateModel
{
    [ProtoMember(1)]
    public int chunkX { get; set; }
    [ProtoMember(2)]
    public int chunkZ { get; set; }
    
    [ProtoMember(3)]
    public int blockX { get; set; }
    [ProtoMember(4)]
    public int blockY { get; set; }
    [ProtoMember(5)]
    public int blockZ { get; set; }
    
    [ProtoMember(6)]
    public byte blockId { get; set; }
}