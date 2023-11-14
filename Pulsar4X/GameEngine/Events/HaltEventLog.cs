using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Events;

public class HaltEventLog : IEventLog
{
    private SafeList<Event> _events = new ();
    private List<EventType> _haltsOn;

    private HaltEventLog() { }

    public static HaltEventLog Create(List<EventType> eventTypes)
    {
        return new HaltEventLog()
        {
            _haltsOn = eventTypes
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

    private void OnEvent(Event e)
    {
        // TODO: need to halt the game
        _events.Add(e);
    }
}