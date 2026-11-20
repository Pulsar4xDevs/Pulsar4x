using Pulsar4X.Engine;

namespace Pulsar4X.Blueprints
{
    public class SpeciesBlueprint : Blueprint
    {
        public string Name { get; set; }
        public bool Playable { get; set; } = true;
        /// <summary>Which Client portrait pack draws this species' people (a folder under Portraits/species). Optional; "human" when absent.</summary>
        public string PortraitPack { get; set; } = "human";
        public string BreathableGasSymbol { get; set; }
        public ValueRange Gravity { get; set; }
        public ValueRange Pressure { get; set; }
        public ValueRange Temperature { get; set; }
    }
}