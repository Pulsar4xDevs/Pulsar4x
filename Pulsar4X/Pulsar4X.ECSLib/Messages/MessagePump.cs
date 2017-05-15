using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
        #region Queue Management

        /// <summary>
        /// Enqueues a message to the ECSLib's Incoming Message Queue.
        /// This function requires setting header manually.
        /// </summary>
        [PublicAPI]
        public void EnqueueMessage(string message) => _incomingMessages.Enqueue(message);

        /// <summary>
        /// Enqueues a message to the ECSLib's Incoming Message Queue.
        /// This function will automatically prepend the message header.
        /// </summary>
        [PublicAPI]
        public void EnqueueMessage(IncomingMessageType messageType, AuthenticationToken authToken, string message)
        {
            message = MessageFormatter.GetMessageHeader(messageType, authToken) + message; 
            EnqueueMessage(message);
        }

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

            message = MessageFormatter.GetOutgoingMessageHeader(messageType) + message; 
            EnqueueOutgoingMessage(message);
        }

        internal void EnqueueOutgoingMessage(string message) => _outgoingMessages.Enqueue(message);

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
    }
}
