using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Species : GameEntity
    {
        public double BaseGravity { get; set; }
        public double MinimumGravityConstraint { get; set; }
        public double MaximumGravityConstraint { get; set; }
        public double BasePressure { get; set; }
        public double MinimumPressureConstraint { get; set; }
        public double MaximumPressureConstraint { get; set; }
        public double BaseTemperature { get; set; }
        public double MinimumTemperatureConstraint { get; set; }
        public double MaximumTemperatureConstraint { get; set; }

        public Species()
        {
            // set default values:
            Name = "Human";
            BaseGravity = 1.0;
            MinimumGravityConstraint = 0.1;
            MaximumGravityConstraint = 1.9;
            BasePressure = 1.0;
            MinimumPressureConstraint = 0.4;
            MaximumPressureConstraint = 4.0;
            BaseTemperature = 14.0;
            MinimumTemperatureConstraint = -15.0;
            MaximumTemperatureConstraint = 45.0;

        }

        public double ColonyCost(Planet planet)
        {
            double cost = 1.0;
            cost *= ColonyGravityCost(planet);
            cost *= ColonyPressureCost(planet);
            cost *= ColonyTemperatureCost(planet);
            cost *= ColonyGasCost(planet);

            return cost;
        }

        public double ColonyGravityCost(Planet planet)
        {
            return 1;
        }

        public double ColonyPressureCost(Planet planet)
        {
            return 1;
        }

        public double ColonyTemperatureCost(Planet planet)
        {
            double cost = 1;
            return cost;
        }

        public double ColonyGasCost(Planet planet)
        {
            double cost = 1;
            return cost;
        }
    }
}
