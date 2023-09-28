using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// SpeciesDB defines an entity as being a Species.
    /// </summary>
    public class SpeciesDB : BaseDataBlob
    {
        /// <summary>
        /// The gas primary this species breathes.
        /// Defaults to O2
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public string BreathableGasSymbol { get; internal set; }

        /// <summary>
        /// The ideal gravity for this species.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double BaseGravity { get; internal set; }

        /// <summary>
        /// The minimum gravity the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MinimumGravityConstraint { get; internal set; }

        /// <summary>
        /// The maximum gravity the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MaximumGravityConstraint { get; internal set; }

        /// <summary>
        /// The ideal atmospheric pressure for the species
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double BasePressure { get; internal set; }

        /// <summary>
        /// The minimum atmospheric pressure the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MinimumPressureConstraint { get; internal set; }

        /// <summary>
        /// The maximum atmospheric pressure the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MaximumPressureConstraint { get; internal set; }

        /// <summary>
        /// The ideal temperature for the species
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double BaseTemperature { get; internal set; }

        /// <summary>
        /// The minimum temperature the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MinimumTemperatureConstraint { get; internal set; }

        /// <summary>
        /// The maximum temperature the species can tolerate
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MaximumTemperatureConstraint { get; internal set; }

        /// <summary>
        /// Range of temperatures this species can withstand?
        /// </summary>
        /// <remarks>
        /// TODO: Gameplay Review
        /// We should either have BaseTemperature + ToleranceRange, or Min/Max temps, but not both.
        /// Min/Max works best with aurora mechanics.
        /// </remarks>
        [PublicAPI]
        [JsonProperty]
        public double TemperatureToleranceRange { get; internal set; }

        public SpeciesDB() { }

        public SpeciesDB(double baseGravity, double minGravity, double maxGravity, double basePressure, double minPressure, double maxPressure, double baseTemp, double minTemp, double maxTemp, string breathableGas = "O2")
        {
            BaseGravity = baseGravity;
            MinimumGravityConstraint = minGravity;
            MaximumGravityConstraint = maxGravity;

            BasePressure = basePressure;
            MinimumPressureConstraint = minPressure;
            MaximumPressureConstraint = maxPressure;

            BaseTemperature = baseTemp;
            MinimumTemperatureConstraint = minTemp;
            MaximumTemperatureConstraint = maxTemp;

            BreathableGasSymbol = breathableGas;

            TemperatureToleranceRange = maxTemp - minTemp;
        }

        public override object Clone()
        {
            return new SpeciesDB(BaseGravity, MinimumGravityConstraint, MaximumGravityConstraint,
                BasePressure, MinimumPressureConstraint, MaximumPressureConstraint,
                BaseTemperature, MinimumTemperatureConstraint, MaximumTemperatureConstraint, BreathableGasSymbol);
        }
    }
}
