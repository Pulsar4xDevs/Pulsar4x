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
            List<KeyValuePair<Entity, List<ComponentInstance>>> infrastructure = colony.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<PopulationSupportAbilityDB>()).ToList();
            long popSupportValue;

            // @todo: Get colony cost and infrastructure, figure out population cap
            //  Pop Cap = Total Population Support Value / Colony Cost
            // Get total popSupport
            popSupportValue = 0;

            foreach (var installation in infrastructure)
            {
                //if(installations[kvp.Key]
                popSupportValue += installation.Key.GetDataBlob<PopulationSupportAbilityDB>().PopulationCapacity;
            }

            long needsSupport = 0;

            foreach (KeyValuePair<Entity, long> kvp in currentPopulation)
            {
                // count the number of different population groups that need infrastructure support
                if (SpeciesProcessor.ColonyCost(colony, kvp.Key.GetDataBlob<SpeciesDB>()) > 1.0)
                    needsSupport++;
            }

            // find colony cost, divide the population support value by it
            // @todo: Get colony cost, or do I need to calculate it?



            foreach (KeyValuePair<Entity, long> kvp in currentPopulation.ToArray())
            {
                double colonyCost = SpeciesProcessor.ColonyCost(colony, kvp.Key.GetDataBlob<SpeciesDB>());
                long maxPopulation;
                double growthRate;
                long newPop;

                if (colonyCost > 1.0)
                {
                    maxPopulation = popSupportValue / needsSupport;
                    if (currentPopulation[kvp.Key] > maxPopulation) // People will start dying
                    {
                        long excessPopulation = currentPopulation[kvp.Key] - maxPopulation;
                        // @todo: figure out better formula
                        growthRate = -50.0;
                        newPop = (long)(kvp.Value * (1.0 + growthRate));
                        if (newPop < 0)
                            newPop = 0;
                        currentPopulation[kvp.Key] = newPop;
                    }
                    else
                    {
                        // Colony Growth Rate = 20 / (CurrentPop ^ (1 / 3))
                        // Capped at 10% before modifiers for planetary and sector governors, also affected by radiation
                        growthRate = (20.0 / (Math.Pow(kvp.Value, (1.0 / 3.0))));
                        if (growthRate > 10.0)
                            growthRate = 10.0;
                        // TODO: get external factors in population growth (or death)
                        newPop = (long)(kvp.Value * (1.0 + growthRate));
                        if (newPop > maxPopulation)
                            newPop = maxPopulation;
                        if (newPop < 0)
                            newPop = 0;
                        currentPopulation[kvp.Key] = newPop;
                    }
                }
                else
                {
                    // Colony Growth Rate = 20 / (CurrentPop ^ (1 / 3))
                    // Capped at 10% before modifiers for planetary and sector governors, also affected by radiation
                    growthRate = (20.0 / (Math.Pow(kvp.Value, (1.0 / 3.0))));
                    if (growthRate > 10.0)
                        growthRate = 10.0;
                    // TODO: get external factors in population growth (or death)
                    newPop = (long)(kvp.Value * (1.0 + growthRate));
                    if (newPop < 0)
                        newPop = 0;
                    currentPopulation[kvp.Key] = newPop;
                }
            }
        }
    }
}
