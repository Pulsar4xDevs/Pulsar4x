using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.People;


namespace Pulsar4X.Colonies
{
    public class PopulationProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency { get; } = TimeSpan.FromDays(30);
        public TimeSpan FirstRunOffset { get; } = TimeSpan.FromDays(30);
        public Type GetParameterType { get; } = typeof(ColonyInfoDB);

        internal void GrowPopulation(Entity colony)
        {
            // Get current population
            var colonyInfoDB = colony.GetDataBlob<ColonyInfoDB>();
            var currentPopulation = colonyInfoDB.Population;
            var instancesDB = colony.GetDataBlob<ComponentInstancesDB>();
            long popSupportValue = instancesDB.GetPopulationSupportValue(colonyInfoDB.PlanetEntity);

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
                        UpdatePopulation(colonyInfoDB, currentPopulation, id, newPop);
                    }
                    else
                    {
                        growthRate = GrowthPerTick(value);
                        // @todo: get external factors in population growth (or death)
                        newPop = (long)(value * (1.0 + growthRate));
                        if (newPop > maxPopulation)
                            newPop = maxPopulation;
                        if (newPop < 0)
                            newPop = 0;
                        UpdatePopulation(colonyInfoDB, currentPopulation, id, newPop);
                    }
                }
                else
                {
                    growthRate = GrowthPerTick(value);
                    // @todo: get external factors in population growth (or death)
                    newPop = (long)(value * (1.0 + growthRate));
                    if (newPop < 0)
                        newPop = 0;
                    UpdatePopulation(colonyInfoDB, currentPopulation, id, newPop);
                }
            }
        }

        /// <summary>
        /// Growth for one 30-day tick, as a fraction. The Aurora rule is an annual percentage:
        /// 20 / (population in millions)^(1/3), capped at 10 % a year. It used to be applied to
        /// the raw head count as a fraction per tick, which grew a 500,000-person colony 25 %
        /// a month.
        /// </summary>
        internal static double GrowthPerTick(long population)
        {
            if (population <= 0)
                return 0;
            double annualPercent = 20.0 / Math.Pow(population / 1_000_000.0, 1.0 / 3.0);
            if (annualPercent > 10.0)
                annualPercent = 10.0;
            return annualPercent / 100.0 * (30.0 / 365.25);
        }

        internal void ReCalcMaxPopulation(Entity colonyEntity)
        {
            if (!colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB))
                return;
            if (!colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var info) || info.PlanetEntity == null)
                return;
            if (!colonyEntity.TryGetDataBlob<ColonyLifeSupportDB>(out var life))
                return;

            life.MaxPopulation = instancesDB.GetPopulationSupportValue(info.PlanetEntity);
        }

        private void UpdatePopulation(ColonyInfoDB colony ,Dictionary<int, long> population, int id, long newPopulation)
        {
            population[id] = newPopulation;
            
            EventManager.Instance.Publish(
                Event.Create(
                    EventType.PopulationChanged,
                    colony.OwningEntity.StarSysDateTime,
                    $"{colony.OwningEntity.GetName(colony.OwningEntity.FactionOwnerID)} population is now {newPopulation}",
                    colony.OwningEntity.FactionOwnerID,
                    colony.OwningEntity.Manager.ManagerID,
                    colony.OwningEntity.Id
                    ));
        }

        public void Init(Game game)
        {
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            GrowPopulation(entity);
            Pulsar4X.Energy.ColonyPowerProcessor.Recalc(entity);
            Recruitment.Refresh(entity, entity.StarSysDateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var colonies = manager.GetAllDataBlobsOfType<ColonyInfoDB>();

            foreach (var colony in colonies)
            {
                if(colony.OwningEntity != null)
                {
                    GrowPopulation(colony.OwningEntity);
                    // Life support follows the head count without waiting for the daily grid pass.
                    Pulsar4X.Energy.ColonyPowerProcessor.Recalc(colony.OwningEntity);
                    Recruitment.Refresh(colony.OwningEntity, colony.OwningEntity.StarSysDateTime);
                }
            }

            return colonies.Count;
        }

    }
}
