using System;

namespace Pulsar4X.ECSLib
{
    internal static class SpeciesProcessor
    {
        public static double ColonyCost(SystemBodyDB planet, SpeciesDB species)
        {
            double cost = 1.0;

            //cost *= ColonyGravityCost(planet, species);
            cost *= ColonyPressureCost(planet);
            cost *= ColonyTemperatureCost(planet);
            cost *= ColonyGasCost(planet);

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
            throw new NotImplementedException();
            double cost = 0;
            //bool isToxic = planet.Atmosphere.Composition.Keys.Any(gas => gas.IsToxic);
            //if (isToxic)
            {
                cost = 3;
            }
            return cost;
        }

        private static double ColonyPressureCost(SystemBodyDB planet)
        {
            throw new NotImplementedException();
        }

        private static double ColonyTemperatureCost(SystemBodyDB planet)
        {

            //More Math (the | | signs are for Absolute Value in case you forgot)
            //TempColCost = | Ideal Temp - Current Temp | / TRU


            throw new NotImplementedException();
        }

        private static double ColonyGasCost(SystemBodyDB planet)
        {

            throw new NotImplementedException();
        }
    }
}