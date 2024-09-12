using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;


namespace Pulsar4X.Engine
{
    public static class PopulationProcessor
    {
        internal static void GrowPopulation(Entity colony)
        {
            // Get current population
            var currentPopulation = colony.GetDataBlob<ColonyInfoDB>().Population;
            var instancesDB = colony.GetDataBlob<ComponentInstancesDB>();
            long popSupportValue = instancesDB.GetPopulationSupportValue();

            long needsSupport = 0;
            foreach (var (id, value) in currentPopulation)
            {

                var species = colony.Manager.GetGlobalEntityById(id).GetDataBlob<SpeciesDB>();
                // count the number of different population groups that need infrastructure support
                if (species.ColonyCost(colony.GetDataBlob<ColonyInfoDB>().PlanetEntity) > 0.0)
                    needsSupport++;
            }

            // find colony cost, divide the population support value by it
            foreach (var (id, value) in currentPopulation.ToArray())
            {
                var species = colony.Manager.GetGlobalEntityById(id).GetDataBlob<SpeciesDB>();
                double colonyCost = species.ColonyCost(colony.GetDataBlob<ColonyInfoDB>().PlanetEntity);
                long maxPopulation;
                double growthRate;
                long newPop;

                if (colonyCost > 0.0)
                {
                    maxPopulation = (long)((double)(popSupportValue / needsSupport) / colonyCost) ;
                    if (currentPopulation[id] > maxPopulation) // People will start dying
                    {
                        long excessPopulation = currentPopulation[id] - maxPopulation;
                        // @todo: figure out better formula
                        growthRate = -50.0;
                        newPop = (long)(value * (1.0 + growthRate));
                        if (newPop < 0)
                            newPop = 0;
                        currentPopulation[id] = newPop;
                    }
                    else
                    {
                        // Colony Growth Rate = 20 / (CurrentPop ^ (1 / 3))
                        // Capped at 10% before modifiers for planetary and sector governors, also affected by radiation
                        growthRate = (20.0 / (Math.Pow(value, (1.0 / 3.0))));
                        if (growthRate > 10.0)
                            growthRate = 10.0;
                        // @todo: get external factors in population growth (or death)
                        newPop = (long)(value * (1.0 + growthRate));
                        if (newPop > maxPopulation)
                            newPop = maxPopulation;
                        if (newPop < 0)
                            newPop = 0;
                        currentPopulation[id] = newPop;
                    }
                }
                else
                {
                    // Colony Growth Rate = 20 / (CurrentPop ^ (1 / 3))
                    // Capped at 10% before modifiers for planetary and sector governors, also affected by radiation
                    growthRate = (20.0 / (Math.Pow(value, (1.0 / 3.0))));
                    if (growthRate > 10.0)
                        growthRate = 10.0;
                    // @todo: get external factors in population growth (or death)
                    newPop = (long)(value * (1.0 + growthRate));
                    if (newPop < 0)
                        newPop = 0;
                    currentPopulation[id] = newPop;
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
