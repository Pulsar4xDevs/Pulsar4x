using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    internal static class MessageDispatcher
    {
        private static readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();

        static MessageDispatcher()
        {
            // Build the list of IMessageHandlers
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(IMessageHandler)) && !type.IsAbstract))
            {
                var handler = (IMessageHandler)Activator.CreateInstance(type);
                _handlers.Add(handler);
            }
        }

        internal static void Dispatch(Game game, string message)
        {
            // Deconstruct the header.
            IncomingMessageType messageType;
            AuthenticationToken authToken;
            if (!MessagePump.TryDeconstructHeader(ref message, out messageType, out authToken))
            {
                // Message header invalid, notifiy the client.
                game.MessagePump.EnqueueOutgoingMessage(OutgoingMessageType.InvalidMsgRecieved, message);
                return;
            }

            // MessageDispatcher utilizes a modified Chain of Responsibility pattern.
            // Only one handler will handle the message, based on the messageType, but we
            // send it to all of them. Those that can't handle it will simply ignore it.
            // Those that can will deal with it properly.
            bool messageHandled = false;

            if (game.Settings.EnableMultiThreading == true)
            {
                // Using conditional short-circuiting, if the message is already handled it wont be passed to additonal handlers.
                // ReSharper disable once AccessToModifiedClosure (I know, I want to)
                Parallel.ForEach(_handlers, handler => { messageHandled = messageHandled || handler.HandleMessage(game, messageType, authToken, message); });
            }
            else
            {
                foreach (IMessageHandler handler in _handlers)
                {
                    messageHandled = handler.HandleMessage(game, messageType, authToken, message);
                    if (messageHandled)
                    {
                        break;
                    }
                }
            }

            if (!messageHandled)
            {
                game.MessagePump.EnqueueOutgoingMessage(OutgoingMessageType.UnhandledMsgTypeRecieved, message);
            }
        }
    }
}
