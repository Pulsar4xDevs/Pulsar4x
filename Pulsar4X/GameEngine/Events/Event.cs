using System;
using System.Collections.Generic;

namespace Pulsar4X.Events;

public class Event
{
    public EventType EventType { get; private set; }
    public DateTime StarDate { get; private set; }
    public string Message { get; private set; }
    public int? FactionId { get; private set; }
    public string? SystemId { get; private set; }
    public int? EntityId { get; private set; }
    public List<int> ConcernedFactions { get; private set; } = new ();

    private Event() { }

    public static Event Create(
                    EventType eventType,
                    DateTime dateTime,
                    string message,
                    int? factionId = null,
                    string? systemId = null,
                    int? entityId = null,
                    List<int>? concernedFactions = null)
    {
        return new Event()
        {
            EventType = eventType,
            StarDate = dateTime,
            Message = message,
            FactionId = factionId,
            SystemId = systemId,
            EntityId = entityId,
            ConcernedFactions = concernedFactions ?? new List<int>()
        };
    }
}