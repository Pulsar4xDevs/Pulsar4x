using System;

namespace Pulsar4X.ECSLib
{
    internal static class SpeciesProcessor
    {
        public static double ColonyCost(SystemBodyDB planet, SpeciesDB species)
        {
            double cost = 1.0;

            //cost *= ColonyGravityCost(planet, species);
            cost *= ColonyPressureCost(planet, species);
            cost *= ColonyTemperatureCost(planet, species);
            cost *= ColonyGasCost(planet, species);

            return cost;
        }

        private static bool ColonyGravityIsHabitible(SystemBodyDB planet, SpeciesDB species)
        {
            double planetGravity = planet.Gravity;
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
        private static double ColonyToxicityCost(SystemBodyDB planet, SpeciesDB species)
        {
            //Toxic Gasses(CC = 2): Hydrogen(H2), Methane(CH4), Ammonia(NH3), Carbon Monoxide(CO), Nitrogen Monoxide(NO), Hydrogen Sulfide(H2S), Nitrogen Dioxide(NO2), Sulfur Dioxide(SO2)
            //Toxic Gasses(CC = 3): Chlorine(Cl2), Florine(F2), Bromine(Br2), and Iodine(I2)
            //Toxic Gasses at 30% or greater of atm: Oxygen(O2) *

            throw new NotImplementedException();
            double cost = 0;
            //bool isToxic = planet.Atmosphere.Composition.Keys.Any(gas => gas.IsToxic);
            //if (isToxic)
            {
                cost = 3;
            }
            return cost;
        }

        private static double ColonyPressureCost(SystemBodyDB planet, SpeciesDB species)
        {
            throw new NotImplementedException();
        }

        private static double ColonyTemperatureCost(SystemBodyDB planet, SpeciesDB species)
        {
            double cost;
            double idealTemp = species.BaseTemperature;
            double planetTemp = planet.BaseTemperature;  // @todo: find correct temperature after terraforming
            double tempRange = species.TemperatureToleranceRange;

            //More Math (the | | signs are for Absolute Value in case you forgot)
            //TempColCost = | Ideal Temp - Current Temp | / TRU
            cost = Math.Abs(idealTemp - planetTemp) / tempRange;

            return cost;
            // throw new NotImplementedException();
        }

        // @question: how does this differ from the Toxicity cost?
        private static double ColonyGasCost(SystemBodyDB planet, SpeciesDB species)
        {   

            throw new NotImplementedException();
        }
    }
}