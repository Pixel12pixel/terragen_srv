using ProtoBuf;

namespace terragen_server.Netcode.TCP.DataModels;

[ProtoContract]
public class LoginModel
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public int version { get; set; }
    
    [ProtoMember(3)]
    public string UdpAddress { get; set; }
    
}