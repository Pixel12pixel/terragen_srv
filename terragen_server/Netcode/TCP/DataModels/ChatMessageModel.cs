using ProtoBuf;

namespace terragen_server.Netcode.TCP.DataModels;

[ProtoContract]
public class ChatMessageModel
{
    [ProtoMember(1)]
    string message;
}