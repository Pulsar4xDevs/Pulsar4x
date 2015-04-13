using System.Linq;

namespace Pulsar4X.ECSLib.Processors
{
    internal static class SpeciesProcessor
    {
        public static double ColonyCost(PlanetInfoDB planet, SpeciesDB species)
        {
            double cost = 1.0;

            //cost *= ColonyGravityCost(planet, species);
            cost *= ColonyPressureCost(planet);
            cost *= ColonyTemperatureCost(planet);
            cost *= ColonyGasCost(planet);

            return cost;
        }

        private static bool ColonyGravityIsHabitible(PlanetInfoDB planet, SpeciesDB species)
        {
            return planet.SurfaceGravity < species.MaximumGravityConstraint && planet.SurfaceGravity > species.MinimumGravityConstraint;
        }

        /// <summary>
        /// cost should increase with composition. there has to be a more efficent way of doing this too.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="species"></param>
        /// <returns></returns>
        private static double ColonyToxidityCost(PlanetInfoDB planet, SpeciesDB species)
        {
            double cost = 0;
            bool isToxic = planet.Atmosphere.Composition.Keys.Any(gas => gas.IsToxic);
            if (isToxic)
            {
                cost = 3;
            }
            return cost;
        }

        private static double ColonyPressureCost(PlanetInfoDB planet)
        {
            return 1;
        }

        private static double ColonyTemperatureCost(PlanetInfoDB planet)
        {
            return 1;
        }

        private static double ColonyGasCost(PlanetInfoDB planet)
        {
            return 1;
        }
    }
}