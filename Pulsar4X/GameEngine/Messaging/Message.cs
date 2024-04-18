using Pulsar4X.Datablobs;

namespace Pulsar4X.Messaging;

public class Message
{
    public MessageTypes MessageType { get; private set; }
    public int? EntityId { get; private set; }
    public string? SystemId { get; private set; }
    public int? FactionId { get; private set; }
    public BaseDataBlob? DataBlob { get; private set; }

    private Message() { }

    public static Message Create(
        MessageTypes messageType, 
        int? entityId = null, 
        string? systemId = null, 
        int? factionId = null, 
        BaseDataBlob? dataBlob = null)
    {
        return new Message()
        {
            MessageType = messageType,
            EntityId = entityId,
            SystemId = systemId,
            FactionId = factionId,
            DataBlob = dataBlob
        };
    }
}