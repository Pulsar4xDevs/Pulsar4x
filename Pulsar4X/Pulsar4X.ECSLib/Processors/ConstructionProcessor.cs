using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    public static class ConstructionProcessor
    {

        /// <summary>
        /// Initializes this Processor.
        /// </summary>
        internal static void Initialize()
        {
        }

        internal static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {

        }

        internal static void ConstructStuff(Entity colony, Game game, int econTicks)
        {
            JDictionary<Guid, int> mineralStockpile = colony.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            JDictionary<Guid, int> materialStockpile = colony.GetDataBlob<ColonyInfoDB>().RefinedStockpile;
            JDictionary<Guid, int> componentStockpile = colony.GetDataBlob<ColonyInfoDB>().ComponentStockpile;

            ColonyConstructionDB colonyConstruction = colony.GetDataBlob<ColonyConstructionDB>();
            FactionInfoDB factionInfo = colony.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>();


            Dictionary<ConstructionType,int> pointRates = new Dictionary<ConstructionType, int>(colonyConstruction.ConstructionRates);
            int maxPoints = colonyConstruction.PointsPerTick * econTicks;

            List<ConstructionJob> constructionJobs = colonyConstruction.JobBatchList;
            foreach (var batchJob in constructionJobs.ToArray())
            {
                ComponentInfoDB designInfo = factionInfo.ComponentDesigns[batchJob.ItemGuid].GetDataBlob<ComponentInfoDB>();
                ConstructionType conType = batchJob.ConstructionType;
                //total number of resources requred for a single job in this batch
                int resourcePoints = designInfo.MinerialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.MaterialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.ComponentCosts.Sum(item => item.Value);

                while (pointRates[conType] > 0 && maxPoints > 0 && batchJob.NumberCompleted < batchJob.NumberOrdered)
                {
                    //gather availible resorces for this job.
                    
                    ConsumeResources(mineralStockpile, batchJob.MineralsRequired);
                    ConsumeResources(materialStockpile, batchJob.MaterialsRequired);
                    ConsumeResources(componentStockpile, batchJob.ComponentsRequired);

                    int useableResourcePoints = designInfo.MinerialCosts.Sum(item => item.Value) - batchJob.MineralsRequired.Sum(item => item.Value);
                    useableResourcePoints += designInfo.MaterialCosts.Sum(item => item.Value) - batchJob.MaterialsRequired.Sum(item => item.Value);
                    useableResourcePoints += designInfo.ComponentCosts.Sum(item => item.Value) - batchJob.ComponentsRequired.Sum(item => item.Value);
                    //how many construction points each resourcepoint is worth.
                    int pointPerResource = designInfo.BuildPointCost / resourcePoints;
                    
                    //calculate how many construction points each resource we've got stored for this job is worth
                    int pointsToUse = Math.Min(pointRates[conType], maxPoints);
                    pointsToUse = Math.Min(pointsToUse, batchJob.PointsLeft);
                    pointsToUse = Math.Min(pointsToUse, useableResourcePoints * pointPerResource);
                    
                    //construct only enough for the amount of resources we have. 
                    batchJob.PointsLeft -= pointsToUse;
                    pointRates[conType] -= pointsToUse;                    
                    maxPoints -= pointsToUse;

                    if (batchJob.PointsLeft == 0)
                    {
                        BatchJobItemComplete(colony, batchJob,designInfo);
                    }
                }
            }
        }

        private static void BatchJobItemComplete(Entity colonyEntity, ConstructionJob batchJob, ComponentInfoDB designInfo)
        {
            ColonyConstructionDB colonyConstruction = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            batchJob.NumberCompleted++;
            batchJob.PointsLeft = designInfo.BuildPointCost;
            batchJob.MineralsRequired = designInfo.MinerialCosts;
            batchJob.MineralsRequired = designInfo.MaterialCosts;
            batchJob.MineralsRequired = designInfo.ComponentCosts;
            if (batchJob.ConstructionType == ConstructionType.Facility)
            {
                FactionInfoDB factionInfo = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>();
                Entity facilityDesignEntity = factionInfo.ComponentDesigns[batchJob.ItemGuid];
                ColonyInfoDB colonyInfo = colonyEntity.GetDataBlob<ColonyInfoDB>();
                colonyInfo.Installations.SafeValueAdd(facilityDesignEntity,1);
                ReCalcProcessor.ReCalcAbilities(colonyEntity);
            }


            if (batchJob.NumberCompleted == batchJob.NumberOrdered)
            {
                colonyConstruction.JobBatchList.Remove(batchJob);
                if (batchJob.Auto)
                {
                    colonyConstruction.JobBatchList.Add(batchJob);
                }
            }
        }

        private static void ConsumeResources(Dictionary<Guid,int> stockpile, Dictionary<Guid,int> toUse)
        {   
        
            foreach (var kvp in toUse.ToArray())
            {
                if (stockpile.ContainsKey(kvp.Key))
                {
                    int amountUsedThisTick = Math.Min(kvp.Value, toUse[kvp.Key]);
                    toUse[kvp.Key] -= amountUsedThisTick;
                    stockpile[kvp.Key] -= amountUsedThisTick;
                }
            }         
        }

        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcConstructionRate(Entity colonyEntity)
        {
            List<Entity> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Keys.ToList();
            List<Entity> factories = new List<Entity>();
            foreach (var inst in installations)
            {
                if (inst.HasDataBlob<ConstructInstationsAbilityDB>() ||                    
                    inst.HasDataBlob<ConstructShipComponentsAbilityDB>() ||
                    inst.HasDataBlob<ConstructFightersAbilityDB>() ||
                    inst.HasDataBlob<ConstructAmmoAbilityDB>())
      
                    factories.Add(inst);
            }

            JDictionary<ConstructionType, int> typeRate = new JDictionary<ConstructionType, int>{
                {ConstructionType.Ammo, 0}, 
                {ConstructionType.Facility, 0}, 
                {ConstructionType.Fighter, 0}, 
                {ConstructionType.ShipComponent, 0}
            };
            foreach (var factory in factories)
            {
                if (factory.HasDataBlob<ConstructInstationsAbilityDB>())
                {
                    int installationPoints = factory.GetDataBlob<ConstructInstationsAbilityDB>().ConstructionPoints;
                    typeRate.SafeValueAdd(ConstructionType.Facility, installationPoints);
                }
                if (factory.HasDataBlob<ConstructShipComponentsAbilityDB>())
                {
                    int shipComponentPoints = factory.GetDataBlob<ConstructShipComponentsAbilityDB>().ConstructionPoints;
                    typeRate.SafeValueAdd(ConstructionType.Facility, shipComponentPoints);
                }
                if (factory.HasDataBlob<ConstructFightersAbilityDB>())
                {
                    int fighterPoints = factory.GetDataBlob<ConstructFightersAbilityDB>().ConstructionPoints;
                    typeRate.SafeValueAdd(ConstructionType.Facility, fighterPoints);
                }
                if (factory.HasDataBlob<ConstructAmmoAbilityDB>())
                {
                    int ammoPoints = factory.GetDataBlob<ConstructAmmoAbilityDB>().ConstructionPoints;
                    typeRate.SafeValueAdd(ConstructionType.Facility, ammoPoints);
                }

            }
            colonyEntity.GetDataBlob<ColonyConstructionDB>().ConstructionRates = typeRate;
            int maxPoints = 0;
            foreach (var p in typeRate.Values)
            {
                if (p > maxPoints)
                    maxPoints = p;
            }
            colonyEntity.GetDataBlob<ColonyConstructionDB>().PointsPerTick = maxPoints;
        }


        #region PlayerInteraction

        /// <summary>
        /// Adds a job to a colonys ColonyConstructionDB.JobBatchList
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="job"></param>
        [PublicAPI]
        public static void AddJob(Entity colonyEntity, ConstructionJob job)
        {
            ColonyConstructionDB constructingDB = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            FactionInfoDB factionInfo = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>();
            lock (constructingDB.JobBatchList) //prevent threaded race conditions
            {
                //check that this faction does have the design on file. I *think* all this type of construction design will get stored in factionInfo.ComponentDesigns
                if (factionInfo.ComponentDesigns.ContainsKey(job.ItemGuid))
                    constructingDB.JobBatchList.Add(job);
            }
        }

        
        /// <summary>
        /// Moves a job up or down the ColonyRefiningDB.JobBatchList. 
        /// </summary>
        /// <param name="colonyEntity">the colony that's being interacted with</param>
        /// <param name="job">the job that needs re-prioritising</param>
        /// <param name="delta">How much to move it ie: 
        /// -1 moves it down the list and it will be done later
        /// +1 moves it up the list andit will be done sooner
        /// this will safely handle numbers larger than the list size, 
        /// placing the item either at the top or bottom of the list.
        /// </param>
        [PublicAPI]
        public static void ChangeJobPriority(Entity colonyEntity, ConstructionJob job, int delta)
        {
            ColonyConstructionDB constructingDB = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            lock (constructingDB.JobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                if (constructingDB.JobBatchList.Contains(job))
                {
                    int currentIndex = constructingDB.JobBatchList.IndexOf(job);
                    int newIndex = currentIndex + delta;
                    if (newIndex <= 0)
                    {
                        constructingDB.JobBatchList.RemoveAt(currentIndex);
                        constructingDB.JobBatchList.Insert(0, job);
                    }
                    else if (newIndex >= constructingDB.JobBatchList.Count - 1)
                    {
                        constructingDB.JobBatchList.RemoveAt(currentIndex);
                        constructingDB.JobBatchList.Add(job);
                    }
                    else
                    {
                        constructingDB.JobBatchList.RemoveAt(currentIndex);
                        constructingDB.JobBatchList.Insert(newIndex, job);
                    }
                }
            }
        } 
        #endregion
    }
}