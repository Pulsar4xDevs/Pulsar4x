using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class SpeciesDB : BaseDataBlob
    {
        public string SpeciesName { get; set; }
        public double BaseGravity { get; set; }
        public double MinimumGravityConstraint { get; set; }
        public double MaximumGravityConstraint { get; set; }
        public double BasePressure { get; set; }
        public double MinimumPressureConstraint { get; set; }
        public double MaximumPressureConstraint { get; set; }
        public double BaseTemperature { get; set; }
        public double MinimumTemperatureConstraint { get; set; }
        public double MaximumTemperatureConstraint { get; set; }

        public SpeciesDB(string speciesName, double baseGravity, double minGravity, double maxGravity, double basePressure, double minPressure, double maxPressure, double baseTemp, double minTemp, double maxTemp)
            : base()
        {
            // set default values:
            SpeciesName = SpeciesName;
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
    }
}
