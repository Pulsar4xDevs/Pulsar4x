using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Events;

public class FactionEventLog : IEventLog
{
    [JsonProperty]
    private SafeList<Event> _events = new ();

    [JsonProperty]
    private int _factionId;
    private MasterTimePulse _masterTimePulse;
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
        if( EventManager.Instance.HaltsOn.Contains(e.EventType))
            _masterTimePulse.PauseTime(); //this will get called multiple times with multiple factions... shoudl probabily done better...
        
        // We only care about events with _factionId present in some way
        if((e.FactionId == null || _factionId != e.FactionId) && !e.ConcernedFactions.Contains(_factionId))
        {
            return;
        }

        _events.Add(e);
    }

    public SafeList<Event> GetEvents() => _events;
}