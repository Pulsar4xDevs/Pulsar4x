using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

public class ColonyBlueprint : Blueprint
{
    public string Name { get; set; }
    public double? StartingPopulation { get; set; }

    /// <summary>When set with <see cref="OwnerFaction"/>, this colony is placed on that body at game start instead of being a player start option. Body is the system body's display name (NameDB default), e.g. "Luna".</summary>
    public string? Body { get; set; }
    /// <summary>System UniqueID to search, e.g. "system-sol". Omitted searches every loaded system for <see cref="Body"/>.</summary>
    public string? System { get; set; }
    /// <summary>Name of the faction that owns the placed colony. Presence of this field keeps the colony out of the player start list.</summary>
    public string? OwnerFaction { get; set; }
    public string? OwnerAbbreviation { get; set; }
    /// <summary>Stance the player and this faction store toward each other. Friendly or Allied.</summary>
    public string? Stance { get; set; }

    public List<StartingItemBlueprint>? Installations { get; set; }
    public List<StartingItemBlueprint>? Cargo { get; set; }
    public List<string>? ComponentDesigns { get; set; }
    public List<string>? OrdnanceDesigns { get; set; }
    public List<string>? ShipDesigns { get; set; }
    public List<string>? StartingItems { get; set; }
    public List<LaunchQueueBlueprint>? LaunchQueue { get; set; }
    public List<FleetBlueprint>? Fleets { get; set; }

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
    }

    public struct LaunchQueueBlueprint
    {
        public string DesignId { get; set; }
        public string? Name { get; set; }
    }
}