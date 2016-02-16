using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SpeciesDB : BaseDataBlob
    {
        [PublicAPI]
        public double BaseGravity
        {
            get { return _baseGravity; }
            internal set { _baseGravity = value; }
        }
        [JsonProperty]
        private double _baseGravity;

        [PublicAPI]
        public double MinimumGravityConstraint
        {
            get { return _minimumGravityConstraint; }
            internal set { _minimumGravityConstraint = value; }
        }
        [JsonProperty]
        private double _minimumGravityConstraint;

        [PublicAPI]
        public double MaximumGravityConstraint
        {
            get { return _maximumGravityConstraint; }
            internal set { _maximumGravityConstraint = value; }
        }
        [JsonProperty]
        private double _maximumGravityConstraint;

        [PublicAPI]
        public double BasePressure
        {
            get { return _basePressure; }
            internal set { _basePressure = value; }
        }
        [JsonProperty]
        private double _basePressure;

        [PublicAPI]
        public double MinimumPressureConstraint
        {
            get { return _minimumPressureConstraint; }
            internal set { _minimumPressureConstraint = value; }
        }
        [JsonProperty]
        private double _minimumPressureConstraint;

        [PublicAPI]
        public double MaximumPressureConstraint
        {
            get { return _maximumPressureConstraint; }
            internal set { _maximumPressureConstraint = value; }
        }
        [JsonProperty]
        private double _maximumPressureConstraint;

        [PublicAPI]
        public double BaseTemperature
        {
            get { return _baseTemperature; }
            internal set { _baseTemperature = value; }
        }
        [JsonProperty]
        private double _baseTemperature;

        [PublicAPI]
        public double MinimumTemperatureConstraint
        {
            get { return _minimumTemperatureConstraint; }
            internal set { _minimumTemperatureConstraint = value; }
        }
        [JsonProperty]
        private double _minimumTemperatureConstraint;

        [PublicAPI]
        public double MaximumTemperatureConstraint
        {
            get { return _maximumTemperatureConstraint; }
            internal set { _maximumTemperatureConstraint = value; }
        }
        [JsonProperty]
        private double _maximumTemperatureConstraint;

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

        public SpeciesDB()
        {
        }

        public SpeciesDB(SpeciesDB speciesDB)
            : this(speciesDB.BaseGravity, speciesDB.MinimumGravityConstraint, speciesDB.MaximumGravityConstraint, 
            speciesDB.BasePressure, speciesDB.MinimumPressureConstraint, speciesDB.MaximumPressureConstraint,
            speciesDB.BaseTemperature, speciesDB.MinimumTemperatureConstraint, speciesDB.MaximumTemperatureConstraint)
        {
        }

        public override object Clone()
        {
            return new SpeciesDB(this);
        }
    }
}
