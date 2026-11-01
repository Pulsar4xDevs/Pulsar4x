using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

/// <summary>
/// The default crate list a fleet Bootstrap order starts from, in priority order. It lives in mod data so
/// a mod can change the list. The player still tweaks it per order. A design a faction does not have is
/// left off when the list is shown.
/// </summary>
public class BootstrapDefaultsBlueprint : Blueprint
{
    public string Name { get; set; } = "";
    public List<BootstrapDefaultLine> Crates { get; set; } = new();
}

/// <summary>One default crate: a component design id and how many.</summary>
public class BootstrapDefaultLine
{
    public string DesignId { get; set; } = "";
    public int Count { get; set; } = 1;
}
