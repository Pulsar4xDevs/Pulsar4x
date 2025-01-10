using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

public class ColonyBlueprint : Blueprint
{
    public string Name { get; set; }
    public double? StartingPopulation { get; set; }

    public List<StartingItemBlueprint>? Installations { get; set; }
    public List<StartingItemBlueprint>? Cargo { get; set; }
    public List<string>? ComponentDesigns { get; set; }
    public List<string>? OrdnanceDesigns { get; set; }
    public List<string>? ShipDesigns { get; set; }
    public List<string>? StartingItems { get; set; }
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
}