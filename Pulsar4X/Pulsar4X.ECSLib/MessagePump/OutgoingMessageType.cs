namespace Pulsar4X.ECSLib
{
    public enum OutgoingMessageType
    {
        // Message Format: 
        // "messageType;message"

        Invalid = 0,

        SubpulseComplete, // message format: "currentDateTime"
        PulseComplete, // message format: "currentDateTime"

        RealTimeStarted, // message format: ""
        RealTimeStopped, // message format: ""


        EntityDataBlobs, // message format: "serializedEntity"

    }
}