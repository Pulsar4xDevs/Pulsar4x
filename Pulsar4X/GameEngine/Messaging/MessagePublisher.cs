using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pulsar4X.Messaging;

public class MessagePublisher
{
    private static readonly MessagePublisher instance = new MessagePublisher();
    private MessagePublisher() {}
    public static MessagePublisher Instance => instance;
    public delegate Task MessageHandler(Message message);

    public event MessageHandler MessageReceived;

    private ConcurrentDictionary<MessageTypes, ConcurrentQueue<Message>> messageQueue = new ();
    private static ConcurrentDictionary<MessageTypes, List<(MessageHandler Handler, Func<Message, bool>? Filter)>> subscribers = new ();

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
        if(!messageQueue.ContainsKey(message.MessageType))
        {
            messageQueue[message.MessageType] = new ConcurrentQueue<Message>();
        }

        messageQueue[message.MessageType].Enqueue(message);
        OnMessageReceived(message);
    }

    private void OnMessageReceived(Message message)
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