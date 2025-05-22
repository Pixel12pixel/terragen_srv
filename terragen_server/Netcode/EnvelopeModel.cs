using ProtoBuf;
using terragen_server.Netcode.TCP.DataModels;

namespace terragen_server.Netcode;

[ProtoContract]
public class EnvelopeModel
{
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;
    
    [ProtoMember(2)]
    public int PocketId { get; set; }
    
    [ProtoMember(3)]
    public int PocketsCount { get; set; }
    
    [ProtoMember(4)]
    public MessageType Type { get; set; }
    
    [ProtoMember(5)]
    public byte[] PayLoad { get; set; }
}

public enum MessageType
{
    UNDEFINED = 0,
    LOGIN = 1,
    LOGIN_RESPONSE = 2,
    DISCONNECT = 3,
    CHAT = 4,
    CHAT_BROADCAST = 5,
    POSITION_UPDATE = 6,
    POSITION_UPDATE_BROADCAST = 7,
    BLOCK_UPDATE = 8,
}