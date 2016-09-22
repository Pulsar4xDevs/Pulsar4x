using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    public static class ConstructionProcessor
    {
        internal static void ConstructStuff(Entity colony, Game game)
        {
            CargoStorageDB stockpile = colony.GetDataBlob<CargoStorageDB>();

            var colonyConstruction = colony.GetDataBlob<ColonyConstructionDB>();
            var factionInfo = colony.GetDataBlob<OwnedDB>().ObjectOwner.GetDataBlob<FactionInfoDB>();


            var pointRates = new Dictionary<ConstructionType, int>(colonyConstruction.ConstructionRates);
            int maxPoints = colonyConstruction.PointsPerTick;

            List<ConstructionJob> constructionJobs = colonyConstruction.JobBatchList;
            foreach (ConstructionJob batchJob in constructionJobs.ToArray())
            {
                var designInfo = factionInfo.ComponentDesigns[batchJob.ItemGuid].GetDataBlob<ComponentInfoDB>();
                ConstructionType conType = batchJob.ConstructionType;
                //total number of resources requred for a single job in this batch
                int resourcePoints = designInfo.MinerialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.MaterialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.ComponentCosts.Sum(item => item.Value);

                while ((pointRates[conType] > 0) && (maxPoints > 0) && (batchJob.NumberCompleted < batchJob.NumberOrdered))
                {
                    //gather availible resorces for this job.
                    
                    ConsumeResources(stockpile, batchJob.MineralsRequired);
                    ConsumeResources(stockpile, batchJob.MaterialsRequired);
                    ConsumeResources(stockpile, batchJob.ComponentsRequired);

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
                        BatchJobItemComplete(colony, stockpile, batchJob,designInfo);
                    }
                }
            }
        }

        private static void BatchJobItemComplete(Entity colonyEntity, CargoStorageDB storage, ConstructionJob batchJob, ComponentInfoDB designInfo)
        {
            var colonyConstruction = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            batchJob.NumberCompleted++;
            batchJob.PointsLeft = designInfo.BuildPointCost;
            batchJob.MineralsRequired = designInfo.MinerialCosts;
            batchJob.MineralsRequired = designInfo.MaterialCosts;
            batchJob.MineralsRequired = designInfo.ComponentCosts;
            var factionInfo = colonyEntity.GetDataBlob<OwnedDB>().ObjectOwner.GetDataBlob<FactionInfoDB>();
            Entity designEntity = factionInfo.ComponentDesigns[batchJob.ItemGuid];
            Entity specificComponent = ComponentInstanceFactory.NewInstanceFromDesignEntity(designEntity, colonyEntity.GetDataBlob<OwnedDB>().ObjectOwner);
            if (batchJob.InstallOn != null)
            {
                if (batchJob.InstallOn == colonyEntity || StorageSpaceProcessor.HasEntity(storage, colonyEntity))
                {
                    EntityManipulation.AddComponentToEntity(batchJob.InstallOn, specificComponent);
                    ReCalcProcessor.ReCalcAbilities(batchJob.InstallOn);
                }

            }
            else
            {
                StorageSpaceProcessor.AddItemToCargo(storage, specificComponent);
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

        /// <summary>
        /// consumes resources in the stockpile, and updates the dictionary.
        /// </summary>
        /// <param name="stockpile"></param>
        /// <param name="toUse"></param>
        private static void ConsumeResources(CargoStorageDB stockpile, IDictionary<Guid, int> toUse)
        {   
            foreach (KeyValuePair<Guid, int> kvp in toUse.ToArray())
            {             
                int amountUsedThisTick = (int)StorageSpaceProcessor.SubtractValue(stockpile, kvp.Key, kvp.Value);
                toUse[kvp.Key] -= amountUsedThisTick;                      
            }         
        }

        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcConstructionRate(Entity colonyEntity)
        {

            //List<Entity> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Keys.ToList();
            
            var factories = new List<Entity>();

            Dictionary<ConstructionType, int> typeRates = new Dictionary<ConstructionType, int>
            {
                {ConstructionType.Ordnance, 0},
                {ConstructionType.Installations, 0},
                {ConstructionType.Fighters, 0},
                {ConstructionType.ShipComponents, 0},
                {ConstructionType.Ships, 0},
            };
            var instancesDB = colonyEntity.GetDataBlob<ComponentInstancesDB>();
            List<KeyValuePair<Entity, PrIwObsList<Entity>>> factoryEntities = instancesDB.SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<ConstructionAtbDB>()).ToList();
            foreach (var factoryDesignList in factoryEntities)
            {
                foreach (var factoryInstance in factoryDesignList.Value)
                {
                    //todo check if it's damaged, check if it's enabled, check if there's enough workers here to.
                    foreach (var item in factoryDesignList.Key.GetDataBlob<ConstructionAtbDB>().InternalConstructionPoints)
                    {
                        typeRates.SafeValueAdd(item.Key, item.Value);
                    }
                }
            }


            colonyEntity.GetDataBlob<ColonyConstructionDB>().ConstructionRates = typeRates;
            int maxPoints = 0;
            foreach (int p in typeRates.Values)
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
            var constructingDB = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            var factionInfo = colonyEntity.GetDataBlob<OwnedDB>().ObjectOwner.GetDataBlob<FactionInfoDB>();
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
            var constructingDB = colonyEntity.GetDataBlob<ColonyConstructionDB>();
            lock (constructingDB.JobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                if (constructingDB.JobBatchList.Contains(job))
                {
                    var currentIndex = constructingDB.JobBatchList.IndexOf(job);
                    var newIndex = currentIndex + delta;
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