using ProtoBuf;

namespace terragen_server.Netcode.UDP.DataModels;

[ProtoContract]
public class PositionUpdateBroadcastModel
{
    [ProtoMember(1)]
    public string PlayerName { get; set; }
    
    [ProtoMember(2)]
    public float X { get; set; }
    
    [ProtoMember(3)]
    public float Y { get; set; }
    
    [ProtoMember(4)]
    public float Z { get; set; }
    
    [ProtoMember(5)]
    public float RotationX { get; set; }
}