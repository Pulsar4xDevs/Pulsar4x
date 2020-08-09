using System;
using System.Collections.Generic;
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
            return SpeciesProcessor.ColonyCost(planet, species);
        }
    }
}
