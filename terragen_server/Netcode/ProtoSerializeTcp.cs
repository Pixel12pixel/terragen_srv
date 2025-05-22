using ProtoBuf;
using terragen_server.Netcode.TCP.DataModels;

namespace terragen_server.Netcode;

public class ProtoSerializeTcp()
{
    private const int ENVELOPE_HEADER_SIZE = 50;

    private readonly int PacketSize;

    private readonly Dictionary<string, List<EnvelopeModel>> _incompleteMessages = new();

    public ProtoSerializeTcp(int packetSize) : this()
    {
        PacketSize = packetSize;
    }

    /// <summary>
    /// Serializes an object into a list of byte arrays, each representing a packet.
    /// </summary>
    /// <param name="obj">object of given class</param>
    /// <param name="messageType">type of object</param>
    /// <typeparam name="T">class of object</typeparam>
    /// <returns>list of bytes tables</returns>
    public List<byte[]> Serialize<T>(T obj, MessageType messageType)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);

        var bytes = stream.ToArray();
        var len = bytes.Length;

        var result = new List<byte[]>();

        if (len > PacketSize - ENVELOPE_HEADER_SIZE)
        {
            var packetsCount = (int)Math.Ceiling((double)len / (PacketSize - ENVELOPE_HEADER_SIZE));
            var id = Guid.NewGuid().ToString();
            var offset = 0;

            for (int i = 0; i < packetsCount; i++)
            {
                var chunkSize = Math.Min(PacketSize - ENVELOPE_HEADER_SIZE, len - offset);
                var chunk = new byte[chunkSize];
                Array.Copy(bytes, offset, chunk, 0, chunkSize);

                var envelope = new EnvelopeModel
                {
                    Id = id,
                    PocketId = i,
                    PocketsCount = packetsCount,
                    Type = messageType,
                    PayLoad = chunk
                };

                result.Add(SerializeEnvelope(envelope));
                offset += chunkSize;
            }
        }
        else
        {
            var envelope = new EnvelopeModel
            {
                Id = Guid.NewGuid().ToString(),
                PocketId = 0,
                PocketsCount = 0,
                Type = messageType,
                PayLoad = bytes
            };

            result.Add(SerializeEnvelope(envelope));
        }

        return result;
    }

    /// <summary>
    /// Deserializes a byte array into an object of the specified type.
    /// </summary>
    /// <param name="rawData">raw byte data</param>
    /// <param name="readBytes"></param>
    /// <typeparam name="T">class to deserialize</typeparam>
    /// <returns>object of given class</returns>
    public T? Deserialize<T>(byte[] rawData, int readBytes)
    {
        var data = rawData.Take(readBytes).ToArray();
        
        using var stream = new MemoryStream(data);
        var envelope = Serializer.Deserialize<EnvelopeModel>(stream);

        if (envelope.PocketsCount <= 1)
        {
            using var payloadStream = new MemoryStream(envelope.PayLoad);
            return Serializer.Deserialize<T>(payloadStream);
        }
        
        if (!_incompleteMessages.TryGetValue(envelope.Id, out var list))
        {
            list = new List<EnvelopeModel>(new EnvelopeModel[envelope.PocketsCount]);
            _incompleteMessages[envelope.Id] = list;
        }

        list[envelope.PocketId] = envelope;
        
        if (list.All(e => e != null))
        {
            var fullPayload = list
                .OrderBy(e => e.PocketId)
                .SelectMany(e => e.PayLoad)
                .ToArray();

            _incompleteMessages.Remove(envelope.Id);

            using var payloadStream = new MemoryStream(fullPayload);
            return Serializer.Deserialize<T>(payloadStream);
        }

        return default;
    }
    
    /// <summary>
    /// Deserializes a byte array into an object of the specified type.
    /// </summary>
    /// <param name="data">raw data</param>
    /// <returns>object</returns>
    public object? Deserialize(byte[] rawData, int readBytes)
    {
        var data = rawData.Take(readBytes).ToArray();
        
        using var stream = new MemoryStream(data);
        var envelope = Serializer.Deserialize<EnvelopeModel>(stream);

        if (envelope.PocketsCount <= 1)
        {
            return DeserializePayload(envelope.Type, envelope.PayLoad);
        }

        if (!_incompleteMessages.TryGetValue(envelope.Id, out var list))
        {
            list = new List<EnvelopeModel>(new EnvelopeModel[envelope.PocketsCount]);
            _incompleteMessages[envelope.Id] = list;
        }

        list[envelope.PocketId] = envelope;

        if (list.All(e => e != null))
        {
            var fullPayload = list
                .OrderBy(e => e.PocketId)
                .SelectMany(e => e.PayLoad)
                .ToArray();

            _incompleteMessages.Remove(envelope.Id);

            return DeserializePayload(envelope.Type, fullPayload);
        }

        return null;
    }
    
    /// <summary>
    /// Deserializes the payload based on the message type.
    /// </summary>
    /// <param name="messageType">type of message</param>
    /// <param name="payload">message payload</param>
    /// <returns>object of given class</returns>
    private object? DeserializePayload(MessageType messageType, byte[] payload)
    {
        using var stream = new MemoryStream(payload);

        return messageType switch
        {
            MessageType.LOGIN => Serializer.Deserialize<LoginModel>(stream),
            MessageType.DISCONNECT => Serializer.Deserialize<DisconnectModel>(stream),
            MessageType.CHAT => Serializer.Deserialize<ChatMessageModel>(stream),
            MessageType.BLOCK_UPDATE => Serializer.Deserialize<BlockUpdateModel>(stream),
            _ => null
        };
    }

    /// <summary>
    /// Serializes the envelope into a byte array.
    /// </summary>
    /// <param name="envelope">envelope model</param>
    /// <returns>serialized byte array</returns>
    private byte[] SerializeEnvelope(EnvelopeModel envelope)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, envelope);
        return stream.ToArray();
    }
    
    /*
    private const int ENVELOPE_HEADER_SIZE = 50;
    
    public List<byte[]> Serialize<T>(T obj, MessageType messageType)
    {
        using var stream = new MemoryStream();
        
        Serializer.Serialize(stream, obj);
        
        var bytes = stream.ToArray();
        
        var len = bytes.Length;

        var result = new List<byte[]>();
        
        
        
        if (len > PacketSize - ENVELOPE_HEADER_SIZE)
        {
            var packetsCount = (int)Math.Ceiling((double)len / PacketSize);
            var id = Guid.NewGuid().ToString();
            var offset = 0;
            for (var i = 0; i < packetsCount; i++)
            {
                var packetSize = Math.Min(PacketSize - ENVELOPE_HEADER_SIZE, len - offset);
                var packetBytes = new byte[packetSize];
                Array.Copy(bytes, offset, packetBytes, 0, packetSize);
                
                var serializedEnvelope = SerializeEnvelope(new EnvelopeModel()
                {
                    Id = id,
                    PocketId = i,
                    PocketsCount = packetsCount,
                    Type = messageType,
                    PayLoad = packetBytes
                });
                
                result.Add(serializedEnvelope);
                
                offset += packetSize;
            }
        }
        else
        {
            var serializedEnvelope = SerializeEnvelope(new EnvelopeModel()
            {
                Id = Guid.NewGuid().ToString(),
                PocketId = 0,
                PocketsCount = 0,
                Type = messageType,
                PayLoad = bytes
            });
            
            result.Add(serializedEnvelope);
        }
        
        return result;
    }

    private byte[] SerializeEnvelope(EnvelopeModel envelope)
    {
        using var stream = new MemoryStream();
        
        Serializer.Serialize(stream, envelope);
        
        return stream.ToArray();
    }

    
    public T Deserialize<T>(byte[] data, int dataSize = 0)
    {
        using var stream = new MemoryStream(data);
        
        var envelope = Serializer.Deserialize<EnvelopeModel>(stream);
        
        var bytes = new byte[envelope.PayLoad.Length];
        Array.Copy(envelope.PayLoad, bytes, envelope.PayLoad.Length);
        
        return DeserializePayload<T>(bytes);
    }
    
    
    public T DeserializePayload<T>(byte[] data)
    {
        var len = BitConverter.ToUInt16(data, 0);
        var bytes = new byte[len];
        Array.Copy(data, 2, bytes, 0, len);
        using var stream = new MemoryStream(bytes);
        return Serializer.Deserialize<T>(stream);
    }
    */
}