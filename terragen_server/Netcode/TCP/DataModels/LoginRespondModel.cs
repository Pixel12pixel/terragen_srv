using ProtoBuf;

namespace terragen_server.Netcode.TCP.DataModels;

[ProtoContract]
public class LoginRespondModel
{
    [ProtoMember(1)]
    public bool success;
    
    [ProtoMember(2)]
    public int udpPort;
}