using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// CommandBytes indicate the type of command being sent.
    /// </summary>
    public enum CommandByte : byte
    {
        NewGame,    // Indicates the data pertains to creating a new game.
        SaveGame,   // Indicates the data pertains to saving the game.
        LoadGame,   // Indicates the data pertains to loading the game.
        Pulse,      // Indicates the data pertains to pulse execution.
        Quit,       // Indicates the data pertains to quitting the current session.
        Echo,       // Used for testing. Should be immediately returned to sender.
    }

    public enum StatusByte : byte
    {
        Start,
        Started,
        Error,
        Failed,
        Abort,
        Aborted,
        Interrupted,
        Completed,
    }

    /// <summary>
    /// Structure used to pass messages between the UI and Library.
    /// </summary>
    public struct Message
    {
        public Guid ClientGuid;
        public CommandByte Type;
        public StatusByte Status;
        public object Data;
    }

    /// <summary>
    /// Interface for Server-side session communications.
    /// A single library can host multiple IServerTransportLayer.
    /// </summary>
    public interface IServerTransportLayer
    {
        IClientTransportLayer Client { get; }

        /// <summary>
        /// Processes a single message from the client.
        /// Returns true if there are further messages to process.
        /// </summary>
        bool ProcessMessage();

        void SendStatusUpdate(CommandByte command, StatusByte status, string data);
        void SendDataBlob(BaseDataBlob dataBlob);
        void SendSystemEntities(StarSystem system);
        void SendFactions();
    }

    /// <summary>
    /// Interface for Client-side session communications.
    /// A single UI usually only hosts a single IClientTransportLayer at a time.
    /// </summary>
    public interface IClientTransportLayer
    {
        Guid ClientGuid { get; }

        /// <summary>
        /// Processes a single message from the library.
        /// Returns true if there are further messages to process.
        /// </summary>
        bool ProcessMessage();

        string RequestStatusUpdate(CommandByte status);
        BaseDataBlob RequestDataBlob(Guid entityGuid, int typeIndex);
        List<Guid> RequestSystemEntities(StarSystem system);
        List<Guid> RequestFactions();
        void SendCommand(CommandByte command, StatusByte status, string data);
    }

    public interface IProcessLayer
    {
        /// <summary>
        /// Used to check for messages.
        /// Returns true if further messages await processing.
        /// </summary>
        bool ProcessMessages();

        void ProcessNewGameMessage(Message currentMessage);
        void ProcessSaveMessage(Message currentMessage);
        void ProcessLoadMessage(Message currentMessage);
        void ProcessPulseMessage(Message currentMessage);
        void SendStatusUpdate(CommandByte command, StatusByte status, string data = null);
        void ProcessQuitMessage(Message currentMessage);
    }
}
