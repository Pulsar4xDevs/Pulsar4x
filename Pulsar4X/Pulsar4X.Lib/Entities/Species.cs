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
        
        /// <summary>
        /// Probably a list of toxic gases for this species.
        /// </summary>
        public List<SpeciesGasConstraint> GasConstraints { get; set; }

        public Species()
        {
            GasConstraints = new List<SpeciesGasConstraint>();

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
            if (planet.SurfaceGravity < MinimumGravityConstraint)
                return 2;
            if (planet.SurfaceGravity > MinimumGravityConstraint)
                return 2;
            return 1;
        }

        public double ColonyPressureCost(Planet planet)
        {
            if (planet.SurfacePressure < MinimumPressureConstraint)
                return 2;
            if (planet.SurfacePressure > MinimumPressureConstraint)
                return 2;
            return 1;
        }

        public double ColonyTemperatureCost(Planet planet)
        {
            double cost = 1;
            if (planet.LowTemperature < MinimumTemperatureConstraint)
                cost *= 2;
            if (planet.MinTemperature < MinimumTemperatureConstraint)
                cost *= 2;
            if (planet.HighTemperature > MaximumTemperatureConstraint)
                cost *= 2;
            if (planet.MaxTemperature > MaximumTemperatureConstraint)
                cost *= 2;
            return cost;
        }

        public double ColonyGasCost(Planet planet)
        {
            double cost = 1;
            foreach (SpeciesGasConstraint constraint in GasConstraints)
            {
                var gas = planet.Gases.FirstOrDefault(x => x.MoleculeId == constraint.Molecule.Id);
                if(gas!=null)
                {
                    if (gas.SurfacePressure < constraint.Minimum)
                        cost *= 2;
                    if (gas.SurfacePressure > constraint.Maximum)
                        cost *= 2;
                }
                else
                {
                    if (constraint.Minimum > 0.0)
                        cost *= 2;
                }
            }
            return cost;
        }
    }
}
