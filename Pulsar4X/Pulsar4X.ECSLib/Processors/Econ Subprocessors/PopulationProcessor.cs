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
            var instancesDB = colony.GetDataBlob<ComponentInstancesDB>();
            long popSupportValue = instancesDB.GetPopulationSupportValue();

            long needsSupport = 0;
            foreach (KeyValuePair<Entity, long> kvp in currentPopulation)
            {
                var species = kvp.Key.GetDataBlob<SpeciesDB>();
                // count the number of different population groups that need infrastructure support
                if (species.ColonyCost(colony.GetDataBlob<ColonyInfoDB>().PlanetEntity) > 0.0)
                    needsSupport++;
            }

            // find colony cost, divide the population support value by it
            foreach (KeyValuePair<Entity, long> kvp in currentPopulation.ToArray())
            {
                var species = kvp.Key.GetDataBlob<SpeciesDB>();
                double colonyCost = species.ColonyCost(colony.GetDataBlob<ColonyInfoDB>().PlanetEntity);
                long maxPopulation;
                double growthRate;
                long newPop;

                if (colonyCost > 0.0)
                {
                    maxPopulation = (long)((double)(popSupportValue / needsSupport) / colonyCost) ;
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
                        // @todo: get external factors in population growth (or death)
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
                    // @todo: get external factors in population growth (or death)
                    newPop = (long)(kvp.Value * (1.0 + growthRate));
                    if (newPop < 0)
                        newPop = 0;
                    currentPopulation[kvp.Key] = newPop;
                }
            }
        }

        public static void ReCalcMaxPopulation(Entity colonyEntity)
        {
            var infrastructure = new List<Entity>();
            var instancesDB = colonyEntity.GetDataBlob<ComponentInstancesDB>();

            //List<KeyValuePair<Entity, PrIwObsList<Entity>>> infrastructureEntities = instancesDB.ComponentsByDesign.GetInternalDictionary().Where(item => item.Key.HasDataBlob<PopulationSupportAtbDB>()).ToList();
            
            long totalMaxPop = instancesDB.GetPopulationSupportValue();

            colonyEntity.GetDataBlob<ColonyLifeSupportDB>().MaxPopulation = totalMaxPop;
        }
    }
}
