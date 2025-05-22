using ProtoBuf;
using terragen_server.Netcode.UDP.DataModels;

namespace terragen_server.Netcode;

public class ProtoSerializeUdp
{
    private const int MAX_UDP_PACKET_SIZE = 1200; // bezpieczny rozmiar UDP pakietu
    private const int ENVELOPE_HEADER_SIZE = 50;  // szacowany narzut serializacji EnvelopeModel

    private readonly int _maxPayloadSize;

    public ProtoSerializeUdp(int maxUdpSize = MAX_UDP_PACKET_SIZE)
    {
        _maxPayloadSize = maxUdpSize - ENVELOPE_HEADER_SIZE;
    }

    /// <summary>
    /// Serializes an object into a single UDP-safe byte array.
    /// </summary>
    public byte[] Serialize<T>(T obj, MessageType messageType)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);
        var payload = stream.ToArray();

        if (payload.Length > _maxPayloadSize)
        {
            throw new InvalidOperationException($"Payload too large for UDP packet. Max: {_maxPayloadSize} bytes, Actual: {payload.Length} bytes");
        }

        var envelope = new EnvelopeModel
        {
            Id = Guid.NewGuid().ToString(),
            PocketId = 0,
            PocketsCount = 1,
            Type = messageType,
            PayLoad = payload
        };

        return SerializeEnvelope(envelope);
    }

    /// <summary>
    /// Deserializes a UDP datagram into a strongly typed object.
    /// </summary>
    public T? Deserialize<T>(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var envelope = Serializer.Deserialize<EnvelopeModel>(stream);

        if (envelope.PocketsCount > 1)
        {
            throw new InvalidOperationException("Fragmented packets not supported in UDP mode.");
        }

        using var payloadStream = new MemoryStream(envelope.PayLoad);
        return Serializer.Deserialize<T>(payloadStream);
    }

    /// <summary>
    /// Deserializes a UDP datagram into a generic object based on message type.
    /// </summary>
    public object? Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var envelope = Serializer.Deserialize<EnvelopeModel>(stream);

        if (envelope.PocketsCount > 1)
        {
            throw new InvalidOperationException("Fragmented packets not supported in UDP mode.");
        }

        return DeserializePayload(envelope.Type, envelope.PayLoad);
    }

    /// <summary>
    /// Deserializes the payload of a UDP datagram based on the message type.
    /// </summary>
    private object? DeserializePayload(MessageType messageType, byte[] payload)
    {
        using var stream = new MemoryStream(payload);

        return messageType switch
        {
            MessageType.POSITION_UPDATE => Serializer.Deserialize<PositionUpdateModel>(stream),
            _ => null
        };
    }

    /// <summary>
    /// Serializes the envelope into a byte array.
    /// </summary>
    private byte[] SerializeEnvelope(EnvelopeModel envelope)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, envelope);
        return stream.ToArray();
    }
}