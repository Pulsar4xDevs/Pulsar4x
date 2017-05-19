﻿namespace Pulsar4X.ECSLib
{
    internal interface IMessageHandler
    {
        /// <summary>
        /// Handles a message from the MessagePump.
        /// </summary>
        /// <returns>True if the message was handled properly.</returns>
        bool HandleMessage(Game game, IncomingMessageType messageType, AuthenticationToken authToken, string message);
    }
}