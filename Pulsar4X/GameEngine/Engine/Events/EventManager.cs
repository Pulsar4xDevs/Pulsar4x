using System;
using System.Collections.Generic;

namespace Pulsar4X.Events;

public class EventManager
{
    private static readonly EventManager instance = new EventManager();

    private Dictionary<EventType, Action<Event>> _subscribers = new();
    private Action<Event>? _allSubscribers;
    private EventManager() {}

    public static EventManager Instance => instance;

    internal void Clear()
    {
        _subscribers.Clear();
        _allSubscribers = null;
    }

    public void Subscribe(EventType eventType, Action<Event> subscriber)
    {
        if(!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = subscriber;
        }
        else
        {
            _subscribers[eventType] += subscriber;
        }
    }

    public void Unsubscribe(EventType eventType, Action<Event> subscriber)
    {
        if(_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] -= subscriber;
        }
    }

    public void SubscribeAll(Action<Event> subscriber)
    {
        _allSubscribers += subscriber;
    }

    public void UnsubscribeAll(Action<Event> subscriber)
    {
        _allSubscribers -= subscriber;
    }

    public void Publish(Event e)
    {
        if (_subscribers.TryGetValue(e.EventType, out var subscriber))
        {
            subscriber?.Invoke(e);
        }

        _allSubscribers?.Invoke(e);
    }
}
