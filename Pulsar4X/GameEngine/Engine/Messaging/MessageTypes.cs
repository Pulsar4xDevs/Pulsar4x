namespace Pulsar4X.Messaging;

public enum MessageTypes
{
    EntityAdded,
    EntityRemoved,
    EntityHidden,
    EntityRevealed,
    DBAdded,
    DBRemoved,
    StarSystemRevealed,
    EntityRenamed,
    FleetReorganized,

    /// <summary>An existing entity's data changed in place (no DataBlob was added or removed).
    /// Published by engine code whose mutations would otherwise be invisible to observers — e.g.
    /// the research processor advancing a lab's tech queue mid-tick.</summary>
    EntityChanged,

    /// <summary>An entity's order queue (or a fleet's standing orders) changed — queued, cancelled,
    /// paused, or wholesale-replaced. Published wherever the queue mutates outside a clock advance so
    /// observers refresh even while the sim is paused. Carries the holder's entity/system/faction.</summary>
    OrdersChanged,
}