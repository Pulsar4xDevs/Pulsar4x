using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SpeciesDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        // The ideal gravity for the species
        public double BaseGravity { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The minimum gravity the species can tolerate
        public double MinimumGravityConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The maximum gravity the species can tolerate
        public double MaximumGravityConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The ideal atmospheric pressure for the species
        public double BasePressure { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The minimum atmospheric pressure the species can tolerate
        public double MinimumPressureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The maximum atmospheric pressure the species can tolerate
        public double MaximumPressureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The ideal temperature for the species
        public double BaseTemperature { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The minimum temperature the species can tolerate
        public double MinimumTemperatureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        // The maximum temperature the species can tolerate
        public double MaximumTemperatureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double TemperatureToleranceRange { get; internal set; }

        public SpeciesDB(double baseGravity, double minGravity, double maxGravity, double basePressure, double minPressure, double maxPressure, double baseTemp, double minTemp, double maxTemp)
        {
            // set default values:
            BaseGravity = baseGravity;
            MinimumGravityConstraint = minGravity;
            MaximumGravityConstraint = maxGravity;
            BasePressure = basePressure;
            MinimumPressureConstraint = minPressure;
            MaximumPressureConstraint = maxPressure;
            BaseTemperature = baseTemp;
            MinimumTemperatureConstraint = minTemp;
            MaximumTemperatureConstraint = maxTemp;

        }

        public SpeciesDB() { }

        public override object Clone()
        {
            return new SpeciesDB(BaseGravity, MinimumGravityConstraint, MaximumGravityConstraint,
                BasePressure, MinimumPressureConstraint, MaximumPressureConstraint,
                BaseTemperature, MinimumTemperatureConstraint, MaximumTemperatureConstraint);
        }
    }
}
