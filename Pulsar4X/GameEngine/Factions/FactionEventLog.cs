using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Events;

namespace Pulsar4X.Factions;

public class FactionEventLog : IEventLog
{
    [JsonProperty]
    private SafeList<Event> _events = new ();

    [JsonProperty]
    private int _factionId;
    private MasterTimePulse _masterTimePulse;
    
    [JsonProperty]
    private SafeList<EventType> _haltsOn = new();
    private FactionEventLog() { }

    public static FactionEventLog Create(int factionId, MasterTimePulse masterTimePulse)
    {
        return new FactionEventLog()
        {
            _factionId = factionId,
            _masterTimePulse = masterTimePulse
        };
    }

    public void Subscribe()
    {
        EventType allEvents = EventTypeHelper.GetAllEventTypes();
        EventManager.Instance.Subscribe(allEvents, OnEvent);
    }

    public void Unsubscribe()
    {
        EventType allEvents = EventTypeHelper.GetAllEventTypes();
        EventManager.Instance.Unsubscribe(allEvents, OnEvent);
    }

    public void OnEvent(Event e)
    {
        // We only care about events with _factionId present in some way
        if((e.FactionId == null || _factionId != e.FactionId) && !e.ConcernedFactions.Contains(_factionId))
        {
            return;
        }

        if (_haltsOn.Contains(e.EventType))
        {
            _masterTimePulse.PauseTime();
        }

        _events.Add(e);
    }

    public void ToggleHaltsOn(EventType eventType)
    {
        if (_haltsOn.Contains(eventType))
        {
            _haltsOn.Remove(eventType);
        }
        else
        {
            _haltsOn.Add(eventType);    
        }
    }

    public bool HaltsOn(EventType eventType) => _haltsOn.Contains(eventType);

    public SafeList<Event> GetEvents() => _events;
}