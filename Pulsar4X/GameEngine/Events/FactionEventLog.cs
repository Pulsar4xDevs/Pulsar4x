using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Events;

public class FactionEventLog : IEventLog
{
    [JsonProperty]
    private SafeList<Event> _events = new ();

    [JsonProperty]
    private int _factionId;

    private FactionEventLog() { }

    public static FactionEventLog Create(int factionId)
    {
        return new FactionEventLog()
        {
            _factionId = factionId
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

        _events.Add(e);
    }

    public SafeList<Event> GetEvents() => _events;
}