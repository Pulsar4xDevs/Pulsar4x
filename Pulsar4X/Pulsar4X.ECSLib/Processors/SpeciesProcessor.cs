using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal static class SpeciesProcessor
    {
        public static double ColonyCost(Entity planet, SpeciesDB species)
        {
            double cost = 1.0;

            cost = Math.Max(cost, ColonyPressureCost(planet, species));
            cost = Math.Max(cost, ColonyTemperatureCost(planet, species));
            cost = Math.Max(cost, ColonyGasCost(planet, species));

            if (!ColonyGravityIsHabitible(planet, species))
                return -1.0; // invalid - cannot create colony here

            return cost;
        }

        private static bool ColonyGravityIsHabitible(Entity planet, SpeciesDB species)
        {
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            double planetGravity = sysBody.Gravity;
            double maxGravity = species.MaximumGravityConstraint;
            double minGravity = species.MinimumGravityConstraint;

            if (planetGravity < minGravity || planetGravity > maxGravity)
                return false;
            return true;

 //           throw new NotImplementedException();
        }

        /// <summary>
        /// cost should increase with composition. there has to be a more efficent way of doing this too.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="species"></param>
        /// <returns></returns>
        private static double ColonyToxicityCost(Entity planet, SpeciesDB species)
        {
            double cost = 1.0;
            double O2Pressure = 0.0;
            double totalPressure = 0.0;
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach(KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                string symbol = kvp.Key.ChemicalSymbol;
                totalPressure += kvp.Value;
                if(kvp.Key.IsToxic)
                {
                    //Toxic Gasses(CC = 2): Hydrogen(H2), Methane(CH4), Ammonia(NH3), Carbon Monoxide(CO), Nitrogen Monoxide(NO), Hydrogen Sulfide(H2S), Nitrogen Dioxide(NO2), Sulfur Dioxide(SO2)
                    //Toxic Gasses(CC = 3): Chlorine(Cl2), Florine(F2), Bromine(Br2), and Iodine(I2)
                    //Toxic Gasses at 30% or greater of atm: Oxygen(O2) *

                    if (symbol == "H2" || symbol == "CH4" || symbol == "NH3" || symbol == "CO" || symbol == "NO" || symbol == "H2S" || symbol == "NO2" || symbol == "SO2")
                        cost = Math.Max(cost, 2.0);
                    if (symbol == "Cl2" || symbol == "F2" || symbol == "Br2" || symbol == "I2")
                        cost = Math.Max(cost, 3.0);
                }
                if (symbol == "O")
                {
                    O2Pressure = kvp.Value;
                }
                                    
            }

            return cost;
        }

        private static double ColonyPressureCost(Entity planet, SpeciesDB species)
        {
            float totalPressure = 0.0f;
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if(atmosphere == null)
            {
                // No atmosphere on the planet, return 1.0?
                // @todo - some other rule for no atmosphere planets?
                return 1.0;
            }

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                totalPressure += kvp.Value;
            }
            
            if(totalPressure > species.MaximumPressureConstraint)
            {
                // @todo: varying value, or straight requirement?
                return 2.0;
            }

            return 1.0;
        }

        private static double ColonyTemperatureCost(Entity planet, SpeciesDB species)
        {
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            double cost;
            double idealTemp = species.BaseTemperature;
            double planetTemp = sysBody.BaseTemperature;  // @todo: find correct temperature after terraforming
            double tempRange = species.TemperatureToleranceRange;

            //More Math (the | | signs are for Absolute Value in case you forgot)
            //TempColCost = | Ideal Temp - Current Temp | / TRU (temps in Kelvin)
            // Converting to Kelvin.  It probably doesn't matter, but just in case
            cost = Math.Abs((idealTemp + 273.15) - (planetTemp + 273.15)) / tempRange;

            return cost;
        }

        // Returns cost based on amount of breathable gas in atmosphere
        private static double ColonyGasCost(Entity planet, SpeciesDB species)
        {
            // @todo: update to check species for its breathable gas

            double cost = 1.0;
            float O2Pressure = 0.0f;
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
                if (symbol == "O")
                    O2Pressure = kvp.Value;
            }

            if (totalPressure >= 4.0f && O2Pressure <= 0.31f)
                cost = cost; // created for the break point

            if (totalPressure == 0.0f) // No atmosphere, obviously not breathable
                return 2.0;

            if (O2Pressure < 0.1f || O2Pressure > 0.3f)  // wrong amount of oxygen
                return 2.0;

            if (O2Pressure / totalPressure > 0.3f) // Oxygen cannot be more than 30% of atmosphere to be breathable
                return 2.0;

            return cost;
        }
    }
}