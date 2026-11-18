using System.Collections.Generic;

namespace Pulsar4X.Blueprints
{
    /// <summary>Multi-faction phase 9: a starting kit for a runtime NPC company, by industry category.</summary>
    public class NpcCompanyKitBlueprint : Blueprint
    {
        /// <summary>mining, refining, farm, or manufacturing.</summary>
        public string Category { get; set; } = "";
        /// <summary>Goods this kit supplies. A mineral not listed still uses the mining kit.</summary>
        public List<string> Goods { get; set; } = new();
        public List<ColonyBlueprint.StartingItemBlueprint> Installations { get; set; } = new();
        public List<ColonyBlueprint.StartingItemBlueprint> Cargo { get; set; } = new();
    }

    /// <summary>Multi-faction phase 9: name template and region prefixes for chartered companies.</summary>
    public class NpcCompanySettingsBlueprint : Blueprint
    {
        /// <summary>Tokens: {Region}, {Good}. Default is "{Region} {Good} Co."</summary>
        public string NameTemplate { get; set; } = "{Region} {Good} Co.";
        /// <summary>Sponsor faction name to the short region used in the company name.</summary>
        public Dictionary<string, string> Regions { get; set; } = new();
    }
}
