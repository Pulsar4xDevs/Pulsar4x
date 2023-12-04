using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Events;

public class HaltEventLog : IEventLog
{
    private SafeList<Event> _events = new ();
    private List<EventType> _haltsOn;
    private MasterTimePulse _masterTimePulse;

    private HaltEventLog() { }

    public static HaltEventLog Create(List<EventType> eventTypes, MasterTimePulse masterTimePulse)
    {
        return new HaltEventLog()
        {
            _haltsOn = eventTypes,
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

    private void OnEvent(Event e)
    {
        _events.Add(e);
        _masterTimePulse.PauseTime();
    }
}