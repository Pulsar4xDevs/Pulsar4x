using Pulsar4X.Engine;

namespace Pulsar4X.Blueprints
{
    public class SpeciesBlueprint : Blueprint
    {
        public string Name { get; set; }
        public bool Playable { get; set; } = true;
        public string BreathableGasSymbol { get; set; }
        public ValueRange Gravity { get; set; }
        public ValueRange Pressure { get; set; }
        public ValueRange Temperature { get; set; }
    }
}