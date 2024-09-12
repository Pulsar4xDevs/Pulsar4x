using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Messaging;

public class MessagePublisher
{
    private static readonly MessagePublisher instance = new MessagePublisher();
    private MessagePublisher() {}
    public static MessagePublisher Instance => instance;
    public delegate Task MessageHandler(Message message);
    private static SafeDictionary<MessageTypes, SafeList<(MessageHandler Handler, Func<Message, bool>? Filter)>> subscribers = new ();

    public void Subscribe(MessageTypes messageType, MessageHandler handler, Func<Message, bool>? filter = null)
    {
        if(!subscribers.ContainsKey(messageType))
        {
            subscribers[messageType] = new ();
        }

        subscribers[messageType].Add((handler, filter));
    }

    public void Unsubscribe(MessageTypes messageType, MessageHandler handler)
    {
        if(subscribers.ContainsKey(messageType))
        {
            subscribers[messageType].RemoveAll(sub => sub.Handler == handler);
        }
    }

    public void Publish(Message message)
    {
        if(subscribers.ContainsKey(message.MessageType))
        {
            foreach(var (handler, filter) in subscribers[message.MessageType])
            {
                if(filter?.Invoke(message) ?? true)
                {
                    Task.Run(() => handler(message));
                }
            }
        }
    }
}