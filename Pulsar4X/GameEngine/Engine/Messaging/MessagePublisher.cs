using System;
using System.Linq;
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

    public async Task Publish(Message message)
    {
        if (subscribers.TryGetValue(message.MessageType, out var handlers))
        {
            var tasks = handlers
                .Where(h => h.Filter?.Invoke(message) ?? true)
                .Select(h => h.Handler(message));

            await Task.WhenAll(tasks);
        }
    }
}