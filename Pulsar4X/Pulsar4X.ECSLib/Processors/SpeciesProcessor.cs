using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.ECSLib.Processors
{
    public static class SpeciesProcessor
    {
#warning Need to sit down and figure out how we're goin to cost all these properly.
        public static double ColonyCost(PlanetInfoDB planet, SpeciesDB species)
        {
            double cost = 1.0;

            //cost *= ColonyGravityCost(planet, species);
            cost *= ColonyPressureCost(planet);
            cost *= ColonyTemperatureCost(planet);
            cost *= ColonyGasCost(planet);

            return cost;
        }

        static bool ColonyGravityIsHabitible(PlanetInfoDB planet, SpeciesDB species) 
        {
            bool isHabitible = false;
            if (planet.SurfaceGravity < species.MaximumGravityConstraint && planet.SurfaceGravity > species.MinimumGravityConstraint)
                isHabitible = true;
            return isHabitible;            
        }

        /// <summary>
        /// cost should increase with composition. there has to be a more efficent way of doing this too. 
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="species"></param>
        /// <returns></returns>
        static double ColonyToxidityCost(PlanetInfoDB planet, SpeciesDB species)
        {
            bool isToxic = false;
            double cost = 0;
            foreach(AtmosphericGasDB gas in planet.Atmosphere.Composition.Keys)
            {
                if (gas.IsToxic)
                {
                    isToxic = true;
                    break;
                }
            }
            if (isToxic == true)
                cost = 3;
            return cost;            
        }

        static double ColonyPressureCost(PlanetInfoDB planet)
        {
            return 1;
        }
        static double ColonyTemperatureCost(PlanetInfoDB planet)
        {
            return 1;
        }
        static double ColonyGasCost(PlanetInfoDB planet)
        {
            return 1;
        }
    }
}
