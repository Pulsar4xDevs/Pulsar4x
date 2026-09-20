namespace Pulsar4X.Blueprints;

public class StarBlueprint : Blueprint
{
    public struct StarInfoBlueprint
    {
        public double? Mass { get; set; }
        public double? Radius { get; set; }
        public double? Age { get; set; }
        public string? Class { get; set; }
        public double? Temperature { get; set; }
        public double? Luminosity { get; set; }
        public string? LuminosityClass { get; set; }
        public string? SpectralType { get; set; }
    }

    public string Name { get; set; }
    public StarInfoBlueprint Info { get; set; }
}