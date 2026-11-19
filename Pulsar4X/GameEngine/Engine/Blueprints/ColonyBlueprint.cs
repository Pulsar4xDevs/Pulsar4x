using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

public class ColonyBlueprint : Blueprint
{
    public string Name { get; set; }
    public double? StartingPopulation { get; set; }
    /// <summary>Trained ship crew waiting in the colony's crew pool at start, of the colony's species. Not part of the population.</summary>
    public long? StartingCrewPool { get; set; }

    /// <summary>
    /// System body's display name (NameDB default), e.g. "Luna" or "Mars".
    /// With <see cref="OwnerFaction"/>, a new faction owns the colony.
    /// With a body and no owner, the player faction that just started gets the colony.
    /// Either way it is not a start option.
    /// </summary>
    public string? Body { get; set; }
    /// <summary>System UniqueID to search, e.g. "system-sol". Omitted searches every loaded system for <see cref="Body"/>.</summary>
    public string? System { get; set; }
    /// <summary>Name of the faction that owns the placed colony. Presence of this field keeps the colony out of the player start list and creates that faction.</summary>
    public string? OwnerFaction { get; set; }
    public string? OwnerAbbreviation { get; set; }
    /// <summary>Stance the player and this faction store toward each other. Friendly or Allied.</summary>
    public string? Stance { get; set; }
    /// <summary>Cash the owning faction starts with. Applied once, when that faction is created.</summary>
    public int StartingFunds { get; set; }
    /// <summary>
    /// When true, a civilian administrator is seated in the first empty admin post.
    /// Requires an installation that creates that post, such as a city hall. Earth leaves this false.
    /// </summary>
    public bool SeatAdministrator { get; set; }
    /// <summary>
    /// When true, a starting civilian is seated as the colony's industry head (<c>IndustrySeatDB</c>), a seat apart
    /// from the city-hall chair, and never the same person as <see cref="SeatAdministrator"/>'s chair holder.
    /// Set per scenario: Early Tech's player Earth leaves it false; Warp Tech Start sets it on player Earth only.
    /// A colony with an <see cref="OwnerFaction"/> (an NPC) is always seated, so a missing or false flag there is
    /// ignored: see <see cref="ShouldSeatIndustryHead"/>.
    /// </summary>
    public bool SeatIndustryHead { get; set; }

    /// <summary>True when creation seats an industry head: the flag, or any NPC colony (one with an owner faction). A method, because the mod loader sets every public property.</summary>
    public bool ShouldSeatIndustryHead() => SeatIndustryHead || !string.IsNullOrEmpty(OwnerFaction);

    /// <summary>
    /// For an <see cref="OwnerFaction"/> colony: unlock only <see cref="StartingItems"/> rather than
    /// every library and tech. Default false keeps the old open-everything placement.
    /// </summary>
    public bool TechFromStartingItems { get; set; }
    /// <summary>Multi-faction phase 6: the owning NPC faction runs its own economy (pays wages, collects tax).</summary>
    public bool NpcEconomy { get; set; }

    public List<StartingItemBlueprint>? Installations { get; set; }
    public List<StartingItemBlueprint>? Cargo { get; set; }
    public List<string>? ComponentDesigns { get; set; }
    public List<string>? OrdnanceDesigns { get; set; }
    public List<string>? ShipDesigns { get; set; }
    public List<string>? StartingItems { get; set; }
    public List<LaunchQueueBlueprint>? LaunchQueue { get; set; }
    public List<FleetBlueprint>? Fleets { get; set; }
    /// <summary>Multi-faction phase 4: bloc market rows seeded into the colony's market policy.</summary>
    public List<MarketRowBlueprint>? Market { get; set; }

    /// <summary>One bloc market row. Use is per day per million people; Min = hold days x use.</summary>
    public class MarketRowBlueprint
    {
        public string CargoId { get; set; } = "";
        /// <summary>"Buy" (demand: consumed, bid with the shortage premium) or "Sell" (surplus at seed ask).</summary>
        public string Role { get; set; } = "Buy";
        public double HoldDays { get; set; } = 30;
        public double BidFactor { get; set; } = 0.75;
        public double UsePerDayPerMillion { get; set; }
        /// <summary>Phase 7a: an absolute daily gap (kg/day) held for HoldDays on top of the per-million use; not consumed by the bloc sink.</summary>
        public double GapPerDay { get; set; }
        /// <summary>Phase 8: what the bloc's abstract industry adds to stock per day per million people (a Sell row);
        /// keeps a manufacturing bloc supplying the others. Held at Max (or 90 days of output).</summary>
        public double OutputPerDayPerMillion { get; set; }
        /// <summary>Phase 7a: the stock farms grow food up to (food rows); 0 = the default cap.</summary>
        public long Max { get; set; }
    }

    public struct StartingItemBlueprint
    {
        public string Id { get; set; }
        public uint Amount { get; set; }
        public string? Type { get; set; }
    }

    public struct FleetBlueprint
    {
        public string Name { get; set; }
        public List<ShipBlueprint>? Ships { get; set; }
    }

    public struct ShipBlueprint
    {
        public string DesignId { get; set; }
        public string Name { get; set; }
        public List<StartingItemBlueprint>? Cargo { get; set; }
        /// <summary>True for a starting ship that flies without a captain (e.g. the Sensor Sat, which has no spare berth).</summary>
        public bool NoCaptain { get; set; }
    }

    public struct LaunchQueueBlueprint
    {
        public string DesignId { get; set; }
        public string? Name { get; set; }
    }
}