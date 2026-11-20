using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace GameEngine.People;

/// <summary>
/// A colony's market-department seat. It holds the entity id of the civilian who is the colony's market head, or -1
/// when the seat is empty. Like the industry seat (<see cref="IndustrySeatDB"/>) the head is a hired civilian, apart from
/// the city-hall chair, and one person holds one post. Use <see cref="MarketSeat"/> to seat and unseat.
/// </summary>
public class MarketSeatDB : BaseDataBlob
{
    public const int Empty = -1;

    /// <summary>Entity id of the seated head, or -1 when empty.</summary>
    [JsonProperty]
    public int HeadId { get; internal set; } = Empty;

    public bool IsSeated => HeadId >= 0;

    public MarketSeatDB() { }

    public MarketSeatDB(MarketSeatDB other)
    {
        HeadId = other.HeadId;
    }

    public override object Clone() => new MarketSeatDB(this);
}
