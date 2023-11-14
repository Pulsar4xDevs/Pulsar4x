using System;
using System.Collections.Generic;

namespace Pulsar4X.Events;

public class EventManager
{
    private static readonly EventManager instance = new EventManager();

    private Dictionary<EventType, Action<Event>> _subscribers = new();

    private EventManager() {}

    public static EventManager Instance => instance;

    public void Clear()
    {
        _subscribers.Clear();
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

    public void Publish(Event e)
    {
        foreach(var (eventType, subscriber) in _subscribers)
        {
            if((e.EventType & eventType) != 0)
            {
                subscriber?.Invoke(e);
            }
        }
    }
}