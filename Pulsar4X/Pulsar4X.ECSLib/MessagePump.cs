using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Pulsar4X.ECSLib
{


    public enum IncomingMessageType
    {
        Invalid = 0,

        // Message Header Format:
        // "messageType;serializedAuthToken"

        // Message Format:
        // "messageHeader;message"

        ExecutePulse,       // message format: "pulseLengthMS"
        StartRealTime,      // message format: "realTimeMultiplier"
        StopRealTime,       // message format: ""

        /// <summary>
        /// Requests a list of dataBlobs that have been edited.
        /// ECSLib will respond with full dataBlob data of all entity dataBlobs
        /// </summary>
        EntityData, // message format: "entityGuid"

        /// <summary>
        /// Requests a list of Entities that have been edited in a system.
        /// ECSLib will respond with Guid's of all edited entities.
        /// </summary>
        EntityChangeList,   // message format: "solarSystemGuid"

        /// <summary>
        /// Reads/Writes data from/to an Entity's Order Queue
        /// </summary>
        EntityOrdersQuery,  // message format: "entityGuid"
        EntityOrdersWrite,  // message format: "entityGuid{,entityGuid...};serializedOrder"
        EntityOrdersClear,  // message format: "entityGuid{,entityGuid...}"

        /// <summary>
        /// Reads/Writes data from/to GameSettings
        /// </summary>
        SettingsQuery,      // message format: ""
        SettingsWrite,      // message format: "serializedSettings"

        /// <summary>
        /// Reads/Writes data from/to DataBlobs
        /// </summary>
        DataBlobWrite,      // message format: "entityGuid;dataBlobTypeName;serializedDB"
        DataBlobQuery,      // message format: "entityGuid;dataBlobTypeName"
        
        /// <summary>
        /// Reads/Writes the FactionAccess Roles.
        /// </summary>
        FactionAccessWrite, // message format: "factionEntityGuid;targetFactionGuid;targetFactionAccess"
        
        /// <summary>
        /// Requests a list of all known SolarSystems
        /// </summary>
        GalaxyQuery,        // message format: ""

        /// <summary>
        /// Request a list of all Entities within a solarSystem.
        /// </summary>
        SolarSystemQuery,   // message format: "systemGuid"
    }

    public enum OutgoingMessageType
    {
        // Message Format: 
        // "messageType;message"
        
        Invalid = 0,

        SubpulseComplete,   // message format: "currentDateTime"
        PulseComplete,      // message format: "currentDateTime"

        RealTimeStarted,    // message format: ""
        RealTimeStopped,    // message format: ""
        

        EntityDataBlobs,    // message format: "serializedEntity"

    }

    /// <summary>
    /// 2-Way message pump utilizing two message queues, Incoming and Outgoing
    /// </summary>
    public class MessagePump
    {
        public int IncomingMessageCount => _incomingMessages.Count;
        public int OutgoingMessageCount => _outgoingMessages.Count;
        
        private readonly Queue<string> _incomingMessages = new Queue<string>();
        private readonly Queue<string> _outgoingMessages = new Queue<string>();
        private readonly Game _game;

        private static readonly Regex _guidListRegex = new Regex("^(?:([\\w]+),?)*;");
        private static readonly Regex _guidRegex = new Regex("^(?:([\\w]+);)");
        private static readonly Regex _messageTypeRegex = new Regex("^(\\d+);");
        private static readonly Regex _authTokenRegex = new Regex("^(\\w+)\n(\\w+)\n");

        internal MessagePump(Game game)
        {
            _game = game;
        }

        internal void ProcessMessages()
        {
            // Attempt to dequeue a message. If none, then return.
            string message;
            if (!TryDequeueIncomingMessage(out message))
                return;

            // Deconstruct the header.
            IncomingMessageType messageType = GetIncomingMessageType(ref message);
            AuthenticationToken authToken = GetAuthToken(ref message);
            
            ProcessMessage(messageType, authToken, message);
        }

        private void ProcessMessage(IncomingMessageType messageType, AuthenticationToken authToken, string message)
        {
            switch (messageType)
            {
                case IncomingMessageType.EntityChangeList:
                    break;
                case IncomingMessageType.EntityOrdersQuery:
                    break;
                case IncomingMessageType.EntityOrdersWrite:
                    break;
                case IncomingMessageType.EntityOrdersClear:
                    break;
                case IncomingMessageType.SettingsQuery:
                    break;
                case IncomingMessageType.SettingsWrite:
                    break;
                case IncomingMessageType.DataBlobWrite:
                    break;
                case IncomingMessageType.DataBlobQuery:
                    break;
                case IncomingMessageType.FactionAccessWrite:
                    break;
                case IncomingMessageType.GalaxyQuery:
                    break;
                case IncomingMessageType.SolarSystemQuery:
                    break;
                case IncomingMessageType.ExecutePulse:
                    break;
                case IncomingMessageType.StartRealTime:
                    _game.GameLoop.StartTime();
                    break;
                case IncomingMessageType.StopRealTime:
                    _game.GameLoop.PauseTime();
                    break;
                case IncomingMessageType.EntityData:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        #region Queue Management

        /// <summary>
        /// Enqueues a message to the ECSLib's Incoming Message Queue.
        /// </summary>
        [PublicAPI]
        public void EnqueueIncomingMessage(string message)
        {
            lock (_incomingMessages)
            {
                _incomingMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// Attempts to dequeue a message from the ECSLib's Incoming Message Queue.
        /// </summary>
        internal bool TryDequeueIncomingMessage([CanBeNull]out string message)
        {
            message = null;
            lock (_incomingMessages)
            {
                if (_incomingMessages.Count == 0)
                    return false;
                message = _incomingMessages.Dequeue();
            }
            return true;
        }

        /// <summary>
        /// Enqueues a message to the ECSLib's Outgoing Message Queue.
        /// </summary>
        internal void EnqueueOutgoingMessage(string message)
        {
            lock (_outgoingMessages)
            {
                _outgoingMessages.Enqueue(message);
            }
        }

        /// <summary>
        /// Attempts to dequeue a message from the ECSLib's Outgoing Message Queue.
        /// </summary>
        [PublicAPI]
        public bool TryDequeueOutgoingMessage([CanBeNull]out string message)
        {
            message = null;
            lock (_outgoingMessages)
            {
                if (_outgoingMessages.Count == 0)
                    return false;
                message = _outgoingMessages.Dequeue();
            }
            return true;
        }

        #endregion Queue Management

        #region Message Deconstruction

        /// <summary>
        /// Retrieves a list of of Guid's from the message.
        /// Strips the detected guid list from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetGuidList(ref string message, out List<Guid> guidList)
        {
            guidList = new List<Guid>();

            // Detect if the passed message has a CSV GuidList at the beginning
            Match match = _guidListRegex.Match(message);
            if (!match.Success)
                return false;
            
            foreach (Capture capture in match.Groups[1].Captures)
            {
                guidList.Add(Guid.Parse(capture.Value));
            }

            // Remove the captured guidList from the message.
            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Retrieves a Guid from the message.
        /// Strips the detected Guid from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetGuid(ref string message, out Guid guid)
        {
            guid = Guid.Empty;
            Match match = _guidRegex.Match(message);
            if (!match.Success)
                return false;

            guid = Guid.Parse(match.Groups[1].Captures[0].Value);

            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Retrieves the IncomingMessageType from the provided message.
        /// Strips the IncomingMessageType from the message for further processing.
        /// </summary>
        internal static IncomingMessageType GetIncomingMessageType(ref string message)
        {
            IncomingMessageType messageType;

            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType) || messageType == IncomingMessageType.Invalid)
                throw new ArgumentException($"Malformed Message. Failed to detect valid IncomingMessageType.\nMessage:\n{message}");

            return messageType;
        }

        /// <summary>
        /// Retrieves the OutgoingMessageType from the provided message.
        /// Strips the OutgoingMessageType from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static OutgoingMessageType GetOutgoingMessageType(ref string message)
        {
            OutgoingMessageType messageType;

            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType) || messageType == OutgoingMessageType.Invalid)
                throw new ArgumentException($"Malformed Message. Failed to detect valid OutgoingMessageType.\nMessage:\n{message}");

            return messageType;
        }

        internal static AuthenticationToken GetAuthToken(ref string message)
        {
            Match match = _authTokenRegex.Match(message);
            if (!match.Success)
                throw new ArgumentException($"Malformed Message. Failed to detect valid AuthenticationToken.\nMessage:\n{message}");

            return new AuthenticationToken(match.Groups[1].Captures[0].Value);
        }

        #endregion Message Deconstruction

        #region Message Construction

        /// <summary>
        /// Returns a validly formatted header with the provided messageType and authToken.
        /// </summary>
        /// <remarks>
        /// Use this function to construct your message headers so the UI doesn't have to worry about how the ECSLib expects the header to be formatted.
        /// </remarks>
        [PublicAPI]
        public static string GetMessageHeader(IncomingMessageType messageType, AuthenticationToken authToken)
        {
            return $"{(int)messageType};{authToken}\n";
        }

        #endregion Message Construction

    }
}
