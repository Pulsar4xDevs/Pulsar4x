namespace Pulsar4X.ECSLib
{
    public class SpeciesDB : BaseDataBlob
    {
        public string SpeciesName;
        public double BaseGravity;
        public double MinimumGravityConstraint;
        public double MaximumGravityConstraint;
        public double BasePressure;
        public double MinimumPressureConstraint;
        public double MaximumPressureConstraint;
        public double BaseTemperature;
        public double MinimumTemperatureConstraint;
        public double MaximumTemperatureConstraint;

        public SpeciesDB(string speciesName, double baseGravity, double minGravity, double maxGravity, double basePressure, double minPressure, double maxPressure, double baseTemp, double minTemp, double maxTemp)
        {
            // set default values:
            SpeciesName = speciesName;
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
            : this(speciesDB.SpeciesName, 
            speciesDB.BaseGravity, speciesDB.MinimumGravityConstraint, speciesDB.MaximumGravityConstraint, 
            speciesDB.BasePressure, speciesDB.MinimumPressureConstraint, speciesDB.MaximumPressureConstraint,
            speciesDB.BaseTemperature, speciesDB.MinimumTemperatureConstraint, speciesDB.MaximumTemperatureConstraint)
        {
        }
    }
}
