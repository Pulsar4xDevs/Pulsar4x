using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

/// <summary>
/// A faction with ships and no colony. <see cref="Body"/> is only where the fleets start.
/// </summary>
public class FactionBlueprint : Blueprint
{
    public string Name { get; set; }
    public string? Abbreviation { get; set; }
    /// <summary>Species blueprint id. The species entity is created in <see cref="System"/>. There is no colony.</summary>
    public string? Species { get; set; }
    /// <summary>
    /// Stance stored both ways toward every other faction that already knows <see cref="System"/>
    /// or owns a colony or ship there. The space master is skipped. A missing entry stays Hostile.
    /// </summary>
    public string? Stance { get; set; }
    /// <summary>System UniqueID, e.g. "system-sol".</summary>
    public string? System { get; set; }
    /// <summary>Display name of the body the fleets orbit.</summary>
    public string? Body { get; set; }
    public int StartingFunds { get; set; }
    public List<string>? ComponentDesigns { get; set; }
    public List<string>? ShipDesigns { get; set; }
    public List<FleetBlueprint>? Fleets { get; set; }

    public struct FleetBlueprint
    {
        public string Name { get; set; }
        public List<ShipBlueprint>? Ships { get; set; }
    }

    public struct ShipBlueprint
    {
        public string DesignId { get; set; }
        public string Name { get; set; }
        public List<CargoBlueprint>? Cargo { get; set; }
    }

    public struct CargoBlueprint
    {
        public string Id { get; set; }
        public uint Amount { get; set; }
        public string? Type { get; set; }
    }
}
