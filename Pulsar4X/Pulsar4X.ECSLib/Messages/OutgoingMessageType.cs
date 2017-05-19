namespace Pulsar4X.ECSLib
{
    public enum OutgoingMessageType
    {
        // Message Format: 
        // "messageType;message"

        Invalid = 0,

        Echo,                       // message format: "messageIn"
        InvalidMsgRecieved,         // message format: "messageIn"
        UnhandledMsgTypeRecieved,   // message format: "messageIn"

        SubpulseComplete,           // message format: "currentDateTime"
        PulseComplete,              // message format: "currentDateTime"

        RealTimeStarted,            // message format: ""
        RealTimeStopped,            // message format: ""


        EntityDataBlobs,            // message format: "serializedEntity"

        GalaxyResponse,             // message format: "starSystemGuid{,starSystemGuid...}"

    }
}