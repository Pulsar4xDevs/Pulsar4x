using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public static class SpeciesDBExtensions
    {
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
                return -1.0; // invalid - cannot create colony here

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
            double cost = 0.0;
            double totalPressure = 0.0;
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                string symbol = kvp.Key.ChemicalSymbol;
                totalPressure += kvp.Value;

                if (kvp.Key.IsHighlyToxic)
                {
                    cost = Math.Max(cost, 3.0);
                }
                else if (kvp.Key.IsToxic)
                {
                    cost = Math.Max(cost, 2.0);
                }
            }

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                if (kvp.Key.IsHighlyToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(kvp.Value / totalPressure * 100.0f, 4);
                    // if current % of atmosphere for this gas is over toxicity threshold
                    if (percentageOfAtmosphere >= kvp.Key.IsHighlyToxicAtPercentage.Value)
                    {
                        cost = Math.Max(cost, 3.0);
                    }
                }
                if (kvp.Key.IsToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(kvp.Value / totalPressure * 100.0f, 4);
                    // if current % of atmosphere for this gas is over toxicity threshold
                    if (percentageOfAtmosphere >= kvp.Key.IsToxicAtPercentage.Value)
                    {
                        cost = Math.Max(cost, 2.0);
                    }
                }
            }

            return cost;
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
                return 2.0;
            }

            var totalPressure = atmosphere.GetAtmosphericPressure();
            if (totalPressure > species.MaximumPressureConstraint)
            {
                // AuroraWiki: If the pressure is too high, the colony cost will be equal to the Atmospheric Pressure
                //             divided by the species maximum pressure with a minimum of 2.0

                return Math.Round(Math.Max(totalPressure / species.MaximumPressureConstraint, 2.0), 6);
            }

            return 0;
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
                return 0;
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
            double cost = 0.0;  // if everything is good then this planet doesnt require infrastructure.
            float speciesBreathablePressure = 0.0f;
            float totalPressure = 0.0f;
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if (atmosphere == null)
            {
                // No atmosphere on the planet, return 2.0?
                // @todo - some other rule for no atmosphere planets?
                return 2.0;
            }

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                string symbol = kvp.Key.ChemicalSymbol;
                totalPressure += kvp.Value;
                if (symbol == species.BreathableGasSymbol)
                    speciesBreathablePressure = kvp.Value;
            }

            //if (totalPressure >= 4.0f && speciesBreathablePressure <= 0.31f)
            //    cost = cost; // created for the break point

            if (totalPressure == 0.0f) // No atmosphere, obviously not breathable
                return 2.0;

            if (speciesBreathablePressure < 0.1f || speciesBreathablePressure > 0.3f)  // wrong amount of species Breathable Gas
                return 2.0;

            if (speciesBreathablePressure / totalPressure > 0.3f) // Species Breathable Gas cannot be more than 30% of atmosphere to be breathable
                return 2.0;

            return cost;
        }
    }
}
