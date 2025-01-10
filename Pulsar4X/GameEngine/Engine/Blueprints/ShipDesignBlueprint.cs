using System.Collections.Generic;

namespace Pulsar4X.Blueprints;

public class ShipDesignBlueprint : Blueprint
{
    public struct ShipArmorBlueprint
    {
        public string Id { get; set; }
        public uint Thickness { get; set; }
    }

    public struct ShipComponentBlueprint
    {
        public string Id { get; set; }
        public uint Amount { get; set; }
    }

    public string Name { get; set; }
    public ShipArmorBlueprint Armor { get; set; }
    public List<ShipComponentBlueprint> Components { get; set; }

}