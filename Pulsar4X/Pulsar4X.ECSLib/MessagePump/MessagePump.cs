using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// 2-Way message pump utilizing two message queues, Incoming and Outgoing
    /// </summary>
    public class MessagePump
    {
        private readonly ConcurrentQueue<string> _incomingMessages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _outgoingMessages = new ConcurrentQueue<string>();

        private static readonly Regex _guidListRegex = new Regex("^(?:([\\w]+),?)*;");
        private static readonly Regex _guidRegex = new Regex("^(?:([\\w]+);)");
        private static readonly Regex _messageTypeRegex = new Regex("^(\\d+);");
        private static readonly Regex _authTokenRegex = new Regex("^(\\w+\n\\w+)\n");

        #region Queue Management

        /// <summary>
        /// Enqueues a message to the ECSLib's Incoming Message Queue.
        /// </summary>
        [PublicAPI]
        public void EnqueueMessage(string message) => _incomingMessages.Enqueue(message);

        /// <summary>
        /// Attempts to peek a message from the ECSLib's Incoming Message Queue. Does not remove the message from the queue.
        /// </summary>
        internal bool TryPeekIncomingMessage(out string message) => _incomingMessages.TryPeek(out message);

        /// <summary>
        /// Attempts to dequeue a message from the ECSLib's Incoming Message Queue.
        /// </summary>
        internal bool TryDequeueIncomingMessage(out string message) => _incomingMessages.TryDequeue(out message);

        /// <summary>
        /// Enqueues a message to the ECSLib's Outgoing Message Queue.
        /// </summary>
        internal void EnqueueOutgoingMessage(OutgoingMessageType messageType, string message)
        {
            if (!Enum.IsDefined(typeof(OutgoingMessageType), messageType))
            {
                throw new InvalidEnumArgumentException(nameof(messageType), (int)messageType, typeof(OutgoingMessageType));
            }

            message = GetOutgoingMessageHeader(messageType) + message; 
            _outgoingMessages.Enqueue(message);
        }

        /// <summary>
        /// Attempts to peek a message from the ECSLib's Outgoing Message Queue. Does not remove the message from the queue.
        /// </summary>
        [PublicAPI]
        public bool TryPeekOutgoingMessage(out string message) => _outgoingMessages.TryPeek(out message);

        /// <summary>
        /// Attempts to dequeue a message from the ECSLib's Outgoing Message Queue.
        /// </summary>
        [PublicAPI]
        public bool TryDequeueOutgoingMessage(out string message) => _outgoingMessages.TryDequeue(out message);
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
            {
                return false;
            }

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
            {
                return false;
            }

            guid = Guid.Parse(match.Groups[1].Captures[0].Value);

            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Retrieves the OutgoingMessageType from the provided message.
        /// Strips the OutgoingMessageType from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetOutgoingMessageType(ref string message, out OutgoingMessageType messageType)
        {
            messageType = OutgoingMessageType.Invalid;
            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType))
            {
                return false;
            }

            // Strip the OutgoingMessageType from the message.
            message = message.Substring(match.Length);
            return true;
        }

        private static bool TryGetIncomingMessageType(ref string message, out IncomingMessageType messageType)
        {
            messageType = IncomingMessageType.Invalid;
            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType))
            {
                return false;
            }

            // Strip the IncomingMessageType from the message.
            message = message.Substring(match.Length);
            return true;
        }

        private static bool TryGetAuthToken(ref string message, out AuthenticationToken authToken)
        {
            authToken = null;
            Match match = _authTokenRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            authToken = new AuthenticationToken(match.Groups[1].Captures[0].Value);
            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Retrieves the IncomingMessageType and AuthenticationToken from the provided message.
        /// Strips the header from the message for additional processing.
        /// </summary>
        internal static bool TryDeconstructHeader(ref string message, out IncomingMessageType messageType, out AuthenticationToken authToken)
        {
            string originalMessage = message;
            authToken = null;

            if (TryGetIncomingMessageType(ref message, out messageType) && TryGetAuthToken(ref message, out authToken))
            {
                return true;
            }

            // Invalid Header. Reconstruct original message.
            message = originalMessage;
            return false;
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
        public static string GetMessageHeader(IncomingMessageType messageType, [NotNull] AuthenticationToken authToken)
        {
            if (authToken == null)
            {
                throw new ArgumentNullException(nameof(authToken));
            }
            if (!Enum.IsDefined(typeof(IncomingMessageType), messageType))
            {
                throw new InvalidEnumArgumentException(nameof(messageType), (int)messageType, typeof(IncomingMessageType));
            }

            return $"{(int)messageType};{authToken}\n";
        }

        private static string GetOutgoingMessageHeader(OutgoingMessageType messageType) => $"{(int)messageType};";
        #endregion Message Construction

    }
}
