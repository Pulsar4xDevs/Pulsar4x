using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Extensions
{
    public static class SpeciesDBExtensions
    {
        public static readonly double UNSURVIVABLE_COST = -1;
        public static readonly double NO_COST = 0;
        public static readonly double MIN_COST = 2;
        public static readonly double MAX_COST = 3;

        public static bool CanSurviveGravityOn(this SpeciesDB species, Entity planet)
        {
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            double planetGravity = sysBody.Gravity;
            double maxGravity = species.MaximumGravityConstraint;
            double minGravity = species.MinimumGravityConstraint;

            if (planetGravity < minGravity || planetGravity > maxGravity)
                return false;
            return true;
        }

        public static double ColonyCost(this SpeciesDB species, Entity planet)
        {
            if (!species.CanSurviveGravityOn(planet))
                return UNSURVIVABLE_COST; // invalid - cannot create colony here

            List<double> costs = new List<double>
            {
                species.ColonyPressureCost(planet),
                species.ColonyTemperatureCost(planet),
                species.ColonyGasCost(planet),
                species.ColonyToxicityCost(planet)
            };

            return costs.Max();
        }

        /// <summary>
        /// Equvalent to the Dangerous Atmosphere Cost in Aurora 4X C#
        /// Cost should increase with composition. there has to be a more efficent way of doing this too.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="species"></param>
        /// <returns></returns>
        public static double ColonyToxicityCost(this SpeciesDB species, Entity planet)
        {
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();
            if(atmosphere == null) return NO_COST;

            double totalPressure = atmosphere.Composition.Values.Sum();

            foreach(var id in atmosphere.Composition.Keys)
            {
                var gas = planet.Manager.Game.AtmosphericGases[id];
                // FIXME: where do the 3.0 and 2.0 come from?
                // If we hit a cost return it
                if(gas.IsHighlyToxic) return MAX_COST;
                if(gas.IsToxic) return MIN_COST;

                if(gas.IsHighlyToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(atmosphere.Composition[id] / totalPressure * 100.0f, 4);
                    if(percentageOfAtmosphere >= gas.IsHighlyToxicAtPercentage.Value) return MAX_COST;
                }

                if(gas.IsToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(atmosphere.Composition[id] / totalPressure * 100.0f, 4);
                    if(percentageOfAtmosphere >= gas.IsToxicAtPercentage.Value) return MIN_COST;
                }
            }

            return NO_COST;
        }

        /// <summary>
        /// Equivalent to Atmospheric Pressure Cost in Aurora4X C#
        /// </summary>
        /// <param name="species"></param>
        /// <param name="planet"></param>
        /// <returns></returns>
        public static double ColonyPressureCost(this SpeciesDB species, Entity planet)
        {
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if (atmosphere == null)
            {
                // No atmosphere on the planet, return 1.0?
                // @todo - some other rule for no atmosphere planets?
                return MIN_COST;
            }

            var totalPressure = atmosphere.GetAtmosphericPressure();
            if (totalPressure > species.MaximumPressureConstraint)
            {
                // AuroraWiki: If the pressure is too high, the colony cost will be equal to the Atmospheric Pressure
                //             divided by the species maximum pressure with a minimum of 2.0

                return Math.Round(Math.Max(totalPressure / species.MaximumPressureConstraint, 2.0), 6);
            }

            return NO_COST;
        }


        /// <summary>
        /// Equivalent to Temperature Factor Cost in Aurora4X C#
        /// </summary>
        public static double ColonyTemperatureCost(this SpeciesDB species, Entity planet)
        {
            // http://aurorawiki.pentarch.org/index.php?title=C-System_Bodies
            // AuroraWiki : The colony cost for a temperature outside the range is Temperature Difference / Temperature Deviation.
            //              So if the deviation was 22 and the temperature was 48 degrees below the minimum, the colony cost would be 48/22 = 2.18
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();

            double planetTemp = sysBody.BaseTemperature;
            if (planet.HasDataBlob<AtmosphereDB>())
            {
                planetTemp = planet.GetDataBlob<AtmosphereDB>().SurfaceTemperature;
            }

            if (planetTemp <= species.MaximumTemperatureConstraint && planetTemp >= species.MinimumTemperatureConstraint)
            {
                return NO_COST;
            }

            //More Math (the | | signs are for Absolute Value in case you forgot)
            //TempColCost = | Ideal Temp - Current Temp | / TRU (temps in Kelvin)
            // Converting to Kelvin.  It probably doesn't matter, but just in case
            var deviation = (species.MaximumTemperatureConstraint - species.MinimumTemperatureConstraint) / 2.0;
            var diff = planetTemp < species.MinimumTemperatureConstraint
                        ? Math.Abs(planetTemp - species.MinimumTemperatureConstraint)
                        : Math.Abs(planetTemp - species.MaximumTemperatureConstraint);

            return diff / deviation;
        }


        /// <summary>
        /// Equivalent to Breathable Atmosphere Cost in Aurora4X C#
        /// </summary>
        public static double ColonyGasCost(this SpeciesDB species, Entity planet)
        {
            // if everything is good then this planet doesnt require infrastructure.
            float speciesBreathablePressure = 0.0f;
            float totalPressure = 0.0f;
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if (atmosphere == null)
            {
                // No atmosphere on the planet, return 2.0?
                // @todo - some other rule for no atmosphere planets?
                return MIN_COST;
            }

            foreach (KeyValuePair<string, float> kvp in atmosphere.Composition)
            {
                var gas = planet.Manager.Game.AtmosphericGases[kvp.Key];
                string symbol = gas.ChemicalSymbol;
                totalPressure += kvp.Value;
                if (symbol == species.BreathableGasSymbol)
                    speciesBreathablePressure = kvp.Value;
            }

            if (totalPressure == 0.0f) // No atmosphere, obviously not breathable
                return MIN_COST;

            if ((speciesBreathablePressure / totalPressure) > 0.3f) // Species Breathable Gas cannot be more than 30% of atmosphere to be breathable
                return MIN_COST;

            return NO_COST;
        }
    }
}
