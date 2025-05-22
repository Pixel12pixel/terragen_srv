using ProtoBuf;

namespace terragen_server.Netcode.UDP.DataModels;

[ProtoContract]
public class PositionUpdateModel
{
    [ProtoMember(1)]
    public string Username { get; set; } = string.Empty;
    
    [ProtoMember(2)]
    public int X { get; set; }
    
    [ProtoMember(3)]
    public int Y { get; set; }
    
    [ProtoMember(4)]
    public int Z { get; set; }
    
    [ProtoMember(5)]
    public float RotationX { get; set; }
}