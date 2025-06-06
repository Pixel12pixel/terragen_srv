using ProtoBuf;

namespace terragen_server.Netcode.TCP.DataModels;

[ProtoContract]
public class ChunkTransmitModel
{
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;
    
    [ProtoMember(2)]
    public byte[] Data { get; set; }
}