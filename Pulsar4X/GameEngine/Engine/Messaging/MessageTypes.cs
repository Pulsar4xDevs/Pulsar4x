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
}