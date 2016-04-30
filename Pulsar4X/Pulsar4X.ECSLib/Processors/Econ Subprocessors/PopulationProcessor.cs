using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    public static class PopulationProcessor
    {
        internal static void GrowPopulation(Entity colony)
        {

            // Get current population
            Dictionary<Entity, long> currentPopulation = colony.GetDataBlob<ColonyInfoDB>().Population;

            foreach (KeyValuePair<Entity, long> kvp in currentPopulation.ToArray())
            {
                if( currentPopulation.ContainsKey(kvp.Key))
                {
                    // Colony Growth Rate = 20 / (CurrentPop ^ (1 / 3))
                    // Capped at 10% before modifiers for planetary and sector governors, also affected by radiation
                    double growthRate = (20.0 / ( Math.Pow(kvp.Value, (1.0/3.0))));
                    if (growthRate > 10.0)
                        growthRate = 10.0;
                    // TODO: get external factors in population growth (or death)
                    long newPop = (long)(kvp.Value * (1.0 + growthRate));
                    if (newPop < 0)
                        newPop = 0;
                    currentPopulation[kvp.Key] = newPop;
                }
            }
            // TODO: Set population to new value  (necessary, or is currentPop a reference to the object in colony?)
        }
    }
}
