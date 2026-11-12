namespace Pulsar4X.Events;

/// <summary>
/// Why the clock stopped for one faction: the first event (or one-shot order) that paused it,
/// and how many other pauses arrived before time resumed. Held on <see cref="Pulsar4X.Engine.MasterTimePulse"/>; not saved.
/// </summary>
public class PauseReason
{
    public Event Event { get; }

    /// <summary>Set when a one-shot order pause stopped the clock.</summary>
    public string? OrderName { get; internal set; }

    public int OthersCount { get; internal set; }

    public PauseReason(Event e, string? orderName)
    {
        Event = e;
        OrderName = orderName;
    }
}
