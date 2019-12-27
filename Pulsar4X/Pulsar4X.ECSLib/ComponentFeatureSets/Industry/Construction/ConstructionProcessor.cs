using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib.Industry
{

    public class ConstructEntitiesProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency {
            get {
                return TimeSpan.FromDays(1);
            }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(3);

        public Type GetParameterType => typeof(ConstructAbilityDB);

        public void Init(Game game)
        {
            //unneeded.
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            ConstructionProcessor.ConstructStuff(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            foreach(var entity in manager.GetAllEntitiesWithDataBlob<ConstructAbilityDB>()) 
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }
    }

    public static class ConstructionProcessor
    {
        internal static void ConstructStuff(Entity colony)
        {
            CargoStorageDB stockpile = colony.GetDataBlob<CargoStorageDB>();
            Entity faction;
            colony.Manager.FindEntityByGuid(colony.FactionOwner, out faction);
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            var colonyConstruction = colony.GetDataBlob<ConstructAbilityDB>();

            var pointRates = new Dictionary<ConstructionType, int>(colonyConstruction.ConstructionRates);
            int maxPoints = colonyConstruction.ConstructionPoints; //TODO: should we get rid of this one? seems like a double up with the pointRates.

            List<ConstructJob> constructionJobs = new List<ConstructJob>(colonyConstruction.JobBatchList.OfType<ConstructJob>());
            foreach (ConstructJob batchJob in constructionJobs.ToArray())
            {
                var designInfo = factionInfo.ComponentDesigns[batchJob.ItemGuid];
                ConstructionType conType = batchJob.ConstructionType;
                //total number of resources requred for a single job in this batch
                int resourcePoints = designInfo.MineralCosts.Sum(item => item.Value);
                resourcePoints += designInfo.MaterialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.ComponentCosts.Sum(item => item.Value);

                //how many construction points each resourcepoint is worth.
                float pointPerResource = (float)designInfo.BuildPointCost / resourcePoints;
                
                while ((pointRates[conType] > 0) && (maxPoints > 0) &&
                       (batchJob.NumberCompleted < batchJob.NumberOrdered))
                {
                    //gather availible resorces for this job.
                    //right now we take all the resources we can, for an individual item in the batch. 
                    //even if we're taking more than we can use in this turn, we're squirriling it away. 

                    IDictionary<Guid, int> batchJobMineralsRequired = batchJob.MineralsRequired;
                    IDictionary<Guid, int> batchJobMaterialsRequired = batchJob.MaterialsRequired;
                    IDictionary<Guid, int> batchJobComponentsRequired = batchJob.ComponentsRequired;
                    ConsumeResources(stockpile, ref batchJobMineralsRequired);
                    ConsumeResources(stockpile, ref batchJobMaterialsRequired);
                    ConsumeResources(stockpile, ref batchJobComponentsRequired);

                    //we calculate the difference between the design resources and the amount of resources we've squirreled away. 
                    
                    // this is the total of the resources that we don't have access to for this item. 
                    int unusableResourceSum = batchJobMineralsRequired.Sum(item => item.Value) + batchJobMaterialsRequired.Sum(item => item.Value) + batchJobComponentsRequired.Sum(item => item.Value);
                    // this is the total resources that can be used on this item. 
                    int useableResourcePoints = resourcePoints - unusableResourceSum;
                    
                    
                    //calculate how many construction points each resource we've got stored for this job is worth
                    float pointsToUse = Math.Min(pointRates[conType], maxPoints);
                    pointsToUse = Math.Min(pointsToUse, batchJob.ProductionPointsLeft);
                    pointsToUse = Math.Min(pointsToUse, useableResourcePoints * pointPerResource);
                    
                    if(pointsToUse < 0)
                        throw new Exception("Can't have negitive production");
                    
                    //construct only enough for the amount of resources we have. 
                    batchJob.ProductionPointsLeft -= (int)Math.Floor(pointsToUse);
                    pointRates[conType] -= (int)Math.Ceiling(pointsToUse);                    
                    maxPoints -= (int)Math.Ceiling(pointsToUse);

                    if (batchJob.ProductionPointsLeft == 0)
                    {
                        BatchJobItemComplete(colony, stockpile, batchJob, designInfo);
                    }

                    if (pointsToUse == 0)
                        break;
                }
            }
        }

        private static void BatchJobItemComplete(Entity constructingEntity, CargoStorageDB storage, ConstructJob batchJob, ComponentDesign designInfo)
        {
            var colonyConstruction = constructingEntity.GetDataBlob<ConstructAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ProductionPointsLeft = designInfo.BuildPointCost;
            batchJob.MineralsRequired = designInfo.MineralCosts;
            batchJob.MineralsRequired = designInfo.MaterialCosts;
            batchJob.MineralsRequired = designInfo.ComponentCosts;

            if (batchJob.InstallOn != null)
            {
                ComponentInstance specificComponent = new ComponentInstance(designInfo);
                if (batchJob.InstallOn == constructingEntity || StorageSpaceProcessor.HasEntity(storage, batchJob.InstallOn.GetDataBlob<CargoAbleTypeDB>()))
                {
                    EntityManipulation.AddComponentToEntity(batchJob.InstallOn, specificComponent);
                    ReCalcProcessor.ReCalcAbilities(batchJob.InstallOn);
                }
            }
            else
            {
                StorageSpaceProcessor.AddCargo(storage, designInfo, 1);
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
        internal static void ConsumeResources(CargoStorageDB fromCargo, ref IDictionary<Guid, int> toUse)
        {   
            foreach (KeyValuePair<Guid, int> kvp in toUse.ToArray())
            {             
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);//fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(kvp.Key);
                
                Guid cargoTypeID = cargoItem.CargoTypeID;
                int amountUsedThisTick = 0;
                if (fromCargo.StoredCargoTypes.ContainsKey(cargoTypeID))
                {
                    if (fromCargo.StoredCargoTypes[cargoTypeID].ItemsAndAmounts.ContainsKey(cargoItem.ID))
                    {
                        amountUsedThisTick = Math.Min((int)fromCargo.StoredCargoTypes[cargoTypeID].ItemsAndAmounts[cargoItem.ID].amount, kvp.Value);
                    }
                }

                if (amountUsedThisTick > 0)
                {
                    StorageSpaceProcessor.RemoveCargo(fromCargo, cargoItem, amountUsedThisTick);
                    toUse[kvp.Key] -= amountUsedThisTick;
                }
            }         
        }

        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcConstructionRate(Entity colonyEntity)
        {

            //List<Entity> installations = constructingEntity.GetDataBlob<ColonyInfoDB>().Installations.Keys.ToList();
            
            var factories = new List<Entity>();

            Dictionary<ConstructionType, int> typeRates = new Dictionary<ConstructionType, int>
            {
                {ConstructionType.Ordnance, 0},
                {ConstructionType.Installations, 0},
                {ConstructionType.Fighters, 0},
                {ConstructionType.ShipComponents, 0},
            };
            var instancesDB = colonyEntity.GetDataBlob<ComponentInstancesDB>();
            
            if (instancesDB.TryGetComponentsByAttribute<ConstructionAtbDB>(out var instances))
            {
                foreach (var instance in instances)
                {
                    float healthPercent = instance.HealthPercent();
                    var designInfo = instance.Design.GetAttribute<ConstructionAtbDB>();
                    foreach (var item in designInfo.InternalConstructionPoints)
                    {
                        typeRates.SafeValueAdd(item.Key, (int)(item.Value * healthPercent));
                    }
                }
            }
            

            colonyEntity.GetDataBlob<ConstructAbilityDB>().ConstructionRates = typeRates;
            int maxPoints = 0;
            foreach (int p in typeRates.Values)
            {
                if (p > maxPoints)
                    maxPoints = p;
            }
            colonyEntity.GetDataBlob<ConstructAbilityDB>().ConstructionPoints = maxPoints;
        }


        #region PlayerInteraction

        /// <summary>
        /// Adds a job to a colonys ColonyConstructionDB.JobBatchList
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="job"></param>
        [PublicAPI]
        public static void AddJob(FactionInfoDB factionInfo, Entity colonyEntity, ConstructJob job)
        {
            var constructingDB = colonyEntity.GetDataBlob<ConstructAbilityDB>();
            //var factionInfo = constructingEntity.GetDataBlob<OwnedDB>().OwnedByFaction.GetDataBlob<FactionInfoDB>();
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
        public static void ChangeJobPriority(Entity colonyEntity, Guid jobID, int delta)
        {
            var constructingDB = colonyEntity.GetDataBlob<ConstructAbilityDB>();
            
            lock (constructingDB.JobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                var job = constructingDB.JobBatchList.Find((obj) => obj.JobID == jobID);
                if (job != null)
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