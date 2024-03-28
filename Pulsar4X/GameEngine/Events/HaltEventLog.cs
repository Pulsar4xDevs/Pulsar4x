using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Events;

public class HaltEventLog : IEventLog
{
    private SafeList<Event> _events = new ();
    private SafeList<EventType> _haltsOn;
    private MasterTimePulse _masterTimePulse;

    private HaltEventLog() { }

    public static HaltEventLog Create(List<EventType> eventTypes, MasterTimePulse masterTimePulse)
    {
        return new HaltEventLog()
        {
            _haltsOn = new(eventTypes),
            _masterTimePulse = masterTimePulse
        };
    }

    public SafeList<Event> GetEvents() => _events;

    public void Subscribe()
    {
        foreach(var type in _haltsOn)
        {
            EventManager.Instance.Subscribe(type, OnEvent);
        }
    }

    public void Unsubscribe()
    {
        foreach(var type in _haltsOn)
        {
            EventManager.Instance.Unsubscribe(type, OnEvent);
        }
    }

    public void AddEvent(EventType eventType)
    {
        _haltsOn.Add(eventType);
        EventManager.Instance.Subscribe(eventType, OnEvent);
    }

    public void RemoveEvent(EventType eventType)
    {
        _haltsOn.Remove(eventType);
        EventManager.Instance.Unsubscribe(eventType, OnEvent);
    }

    public List<EventType> HaltsOn
    {
        get
        {
            return _haltsOn.ToList();
        }
    }

    private void OnEvent(Event e)
    {
        _events.Add(e);
        _masterTimePulse.PauseTime();
    }
}