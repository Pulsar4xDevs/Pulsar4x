using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace GameEngine.People;

/// <summary>
/// A colony's industry-department seat (civilian industry plan, slice 1). It holds the entity id of the
/// civilian who is the colony's industry head, or -1 when the seat is empty. The head comes from the
/// hired-administrator pool (an unassigned <see cref="Pulsar4X.People.CommanderDB"/> civilian); the seat
/// is separate from the city-hall chair in <see cref="AdminSpaceDB"/>, and one person cannot hold both.
/// Use <see cref="IndustrySeat"/> to seat and unseat so the checks and the commander's AssignedTo stay in step.
/// </summary>
public class IndustrySeatDB : BaseDataBlob
{
    public const int Empty = -1;

    /// <summary>Entity id of the seated head, or -1 when empty.</summary>
    [JsonProperty]
    public int HeadId { get; internal set; } = Empty;

    public bool IsSeated => HeadId >= 0;

    public IndustrySeatDB() { }

    public IndustrySeatDB(IndustrySeatDB other)
    {
        HeadId = other.HeadId;
    }

    public override object Clone() => new IndustrySeatDB(this);
}
