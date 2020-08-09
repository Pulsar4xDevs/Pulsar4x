using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class AtmosphereDBExtensions
    {
        public static float GetAtmosphericPressure(this AtmosphereDB atmosphere)
        {
            float totalPressure = 0;
            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                totalPressure += kvp.Value;
            }

            return totalPressure;
        }
        
    }
}
