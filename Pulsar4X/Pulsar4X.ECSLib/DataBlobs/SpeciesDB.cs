using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SpeciesDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public double BaseGravity { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MinimumGravityConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MaximumGravityConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double BasePressure { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MinimumPressureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MaximumPressureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double BaseTemperature { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MinimumTemperatureConstraint { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double MaximumTemperatureConstraint { get; internal set; }

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
