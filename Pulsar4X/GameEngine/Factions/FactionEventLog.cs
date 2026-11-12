using System.Collections.Generic;
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
    private Dictionary<EventType, bool> _haltsOn = new();
    private FactionEventLog() { }

    public static FactionEventLog Create(int factionId, MasterTimePulse masterTimePulse, IReadOnlyDictionary<EventType, bool>? defaultHalts = null)
    {
        var log = new FactionEventLog()
        {
            _factionId = factionId,
            _masterTimePulse = masterTimePulse
        };
        if (defaultHalts != null)
        {
            foreach (var kv in defaultHalts)
                log._haltsOn[kv.Key] = kv.Value;
        }
        return log;
    }

    /// <summary>
    /// <see cref="_masterTimePulse"/> is not serialized. Call this after load so pause works again.
    /// </summary>
    public void AttachTimePulse(MasterTimePulse masterTimePulse)
    {
        _masterTimePulse = masterTimePulse;
    }

    public IReadOnlyDictionary<EventType, bool> HaltSettings => _haltsOn;

    public void Subscribe()
    {
        EventManager.Instance.SubscribeAll(OnEvent);
    }

    public void Unsubscribe()
    {
        EventManager.Instance.UnsubscribeAll(OnEvent);
    }

    public void OnEvent(Event e)
    {
        // We only care about events with _factionId present in some way
        if((e.FactionId == null || _factionId != e.FactionId) && !e.ConcernedFactions.Contains(_factionId))
        {
            return;
        }

        if (HaltsOn(e.EventType) && _masterTimePulse != null)
        {
            _masterTimePulse.PauseTime();
        }

        _events.Add(e);
    }

    public void ToggleHaltsOn(EventType eventType)
    {
        SetHaltsOn(eventType, !HaltsOn(eventType));
    }

    public void SetHaltsOn(EventType eventType, bool halt)
    {
        _haltsOn[eventType] = halt;
    }

    public bool HaltsOn(EventType eventType) => _haltsOn.TryGetValue(eventType, out var on) && on;

    public SafeList<Event> GetEvents() => _events;
}
