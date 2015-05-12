using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Indicates the type of message being sent.
    /// </summary>
    public enum MessageType
    {
        // InQueue
        Quit,                   // terminates the main game loop.
        Save,                   // saves game 
        Load,                   // loads game
        Echo,                   // will be sent straight back to sender. Use for testing.    
        RequestData,            // Used by UI to request data. Will have a List<Guid> of the IDs of the data being requested as well as a message ID.
        RequestSystems,         // Used by the UI to reques the list of star systems, will contain a message ID.
        RequestFactions,        // Used by the UI to request the list of factions, will contain a message ID..
        RequestGameState,       // Used by the UI to request the state of the game, current date etc, will contain a message ID.
        StartPulse,             // Used to start a pulse, should hold the length of the pulse.
        StopPulse,              // Used by the UI to try and abort a pulse. The engin may not be able to comply.

        // OutQueue
        GameStatusUpdate,       // A generic status update from the Game. Data should be a string.
        SaveCompleted,          // Sent to the UI when the lib has finished saving the game.
        LoadCompleted,          // Sent to the UI when the lib has finished Loading.
        ResponseData,           // Response to RequestData, will contain the data requested as well as the message ID.
        ResponseSystems,        // Response to RequestSystems, will contain the list of systems as well as the message ID.
        ResponseFactions,       // Response to RequestFactions, will contain the list of factions as well as the message ID.
        ResponseGameState,      // Response to RequestGameState, will contain the game state as well as the message ID.
        PulseCompleted,         // Used to indicate to the Ui that a pulse has completed successfully.
        PulseInterupted,        // Used to indicate that a pulse was interupteddue to an important event.
        PulseAborted,           // sent in response to a StopPulse message. Indicates the lib aborted the pulse as requested.
    }

    /// <summary>
    /// This class wraps a message sent between UI adn the egnine.
    /// </summary>
    /// <remarks>
    /// The message has a MessageType which helps determins hoe it is processed.
    /// It also has an ID. This id is not necessarily unique. Instead it is used
    /// to relate messages together. For example if the UI send a message requesting 
    /// some data from the engine with an ID of 1 then the message the engine sends in
    /// reply wqould also have the same ID, allowing the UI to better decide how to 
    /// process the message (because it knows which request it is in response too).
    /// The Message also contains a Data field that is used to hold any relevent data.
    /// The dat field can be null if not extra data is required.
    /// </remarks>
    public class Message
    {
        public MessageType Type;
        public Guid MessageID;  
        public object Data;

        public Message(MessageType type, object data)
        {
            Type = type;
            Data = data;
        }

        public Message() { }
    }
}