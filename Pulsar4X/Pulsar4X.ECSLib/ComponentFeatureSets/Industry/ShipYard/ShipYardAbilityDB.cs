using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;


namespace Pulsar4X.ECSLib.Industry
{
    public class ShipYardAbilityDB : BaseDataBlob, IIndustryDB
    {
        
        public int PointsPerTick { get; internal set; }

        public List<int> SlipSizes = new List<int>();
        [JsonProperty]
        public List<JobBase> JobBatchList { get; internal set; }

        public List<ICargoable> GetJobItems(FactionInfoDB factionInfoDB)
        {

                List<ICargoable> designs = new List<ICargoable>();
                foreach (var design in factionInfoDB.ShipDesigns.Values)
                {
                    designs.Add(design);
                }
                return designs;
            
        }


        public ShipYardAbilityDB(ShipYardAbilityDB db)
        {
            PointsPerTick = db.PointsPerTick;
            SlipSizes = new List<int>(db.SlipSizes);
            JobBatchList = db.JobBatchList;
        }

        public override object Clone()
        {
            return new ShipYardAbilityDB(this);
        }
    }
    
    public class ShipYardJob : JobBase
    {

        public ShipClass ShipDesign { get; set; }

        public Dictionary<Guid, int> MineralsRequired { get; internal set; }
        public Dictionary<Guid, int> MaterialsRequired { get; internal set; }
        public Dictionary<Guid, int> ComponentsRequired { get; internal set; }
        /// <summary>
        /// this typicaly should be the designID. (though theoreticaly the instance ID will work if it's only that instance the design requires)
        /// </summary>
        public Dictionary<Guid, int> ShipDesignInstancesRequired { get; internal set; }

        public ShipYardJob()
        {
        }

        public ShipYardJob(Guid designGuid, ShipClass shipdesign, ushort numberOrderd, int jobPoints, bool auto, 
                           Dictionary<Guid,int> mineralCost, Dictionary<Guid, int> matCost, Dictionary<Guid,int> componentCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            Name = shipdesign.Name;
            ShipDesign = shipdesign;
            MineralsRequired = new Dictionary<Guid, int>(mineralCost);
            MaterialsRequired = new Dictionary<Guid, int>(matCost);
            ComponentsRequired = new Dictionary<Guid, int>(componentCost);
        }

        public ShipYardJob(ShipClass shipdesign, ushort numOrdered, bool auto): 
            base(shipdesign.ID, numOrdered, shipdesign.BuildPointCost, auto)
        {
            Name = shipdesign.Name;
            ShipDesign = shipdesign;
            MineralsRequired = shipdesign.MineralCosts;
            MaterialsRequired = shipdesign.MaterialCosts;
            ComponentsRequired = shipdesign.ComponentCosts;
            ShipDesignInstancesRequired = shipdesign.ShipInstanceCost;
        }

        public override void InitialiseJob(FactionInfoDB factionInfo, Entity industryEntity, Guid guid, ushort numberOrderd, bool auto)
        {
            ItemGuid = guid;
            var design = factionInfo.ShipDesigns[ItemGuid];
            ShipDesign = design;
            Name = design.Name;
            MineralsRequired = design.MineralCosts;
            MaterialsRequired = design.MaterialCosts;
            ComponentsRequired = design.ComponentCosts;
            ShipDesignInstancesRequired = design.ShipInstanceCost;
            
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            ProductionPointsLeft = design.BuildPointCost;
            ProductionPointsCost = design.BuildPointCost;
            Auto = auto;
        }

    }

    public static class ShipyardProcessor
    {
        internal static void ConstructStuff(Entity colony)
        {
            CargoStorageDB stockpile = colony.GetDataBlob<CargoStorageDB>();
            Entity faction;
            colony.Manager.FindEntityByGuid(colony.FactionOwner, out faction);
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            var syconstruction = colony.GetDataBlob<ShipYardAbilityDB>();

            
            int maxPoints = syconstruction.PointsPerTick; 

            List<ShipYardJob> constructionJobs = new List<ShipYardJob>(syconstruction.JobBatchList.OfType<ShipYardJob>());
            foreach (ShipYardJob batchJob in constructionJobs.ToArray())
            {
                var designInfo = batchJob.ShipDesign;
                
                //total number of resources requred for a single job in this batch
                int resourcePoints = designInfo.MineralCosts.Sum(item => item.Value);
                resourcePoints += designInfo.MaterialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.ComponentCosts.Sum(item => item.Value);

                //how many construction points each resourcepoint is worth.
                float pointPerResource = (float)designInfo.BuildPointCost / resourcePoints;
                
                while ((maxPoints > 0) && (batchJob.NumberCompleted < batchJob.NumberOrdered))
                {
                    //gather availible resorces for this job.
                    //right now we take all the resources we can, for an individual item in the batch. 
                    //even if we're taking more than we can use in this turn, we're squirriling it away. 

                    IDictionary<Guid, int> batchJobMineralsRequired = batchJob.MineralsRequired;
                    IDictionary<Guid, int> batchJobMaterialsRequired = batchJob.MaterialsRequired;
                    IDictionary<Guid, int> batchJobComponentsRequired = batchJob.ComponentsRequired;
                    IDictionary<Guid, int> batchJobShipsRequired = batchJob.ShipDesignInstancesRequired;
                    ConstructionProcessor.ConsumeResources(stockpile, ref batchJobMineralsRequired);
                    ConstructionProcessor.ConsumeResources(stockpile, ref batchJobMaterialsRequired);
                    ConstructionProcessor.ConsumeResources(stockpile, ref batchJobComponentsRequired);
                    ConstructionProcessor.ConsumeResources(stockpile, ref batchJobShipsRequired);

                    //we calculate the difference between the design resources and the amount of resources we've squirreled away. 
                    
                    // this is the total of the resources that we don't have access to for this item. 
                    int unusableResourceSum = batchJobMineralsRequired.Sum(item => item.Value) + batchJobMaterialsRequired.Sum(item => item.Value) + batchJobComponentsRequired.Sum(item => item.Value);
                    // this is the total resources that can be used on this item. 
                    int useableResourcePoints = resourcePoints - unusableResourceSum;
                    
                    
                    //calculate how many construction points each resource we've got stored for this job is worth
                    float pointsToUse =  maxPoints;
                    pointsToUse = Math.Min(pointsToUse, batchJob.ProductionPointsLeft);
                    pointsToUse = Math.Min(pointsToUse, useableResourcePoints * pointPerResource);
                    
                    if(pointsToUse < 0)
                        throw new Exception("Can't have negitive production");
                    
                    //construct only enough for the amount of resources we have. 
                    batchJob.ProductionPointsLeft -= (int)Math.Floor(pointsToUse);
                                      
                    maxPoints -= (int)Math.Ceiling(pointsToUse);

                    if (batchJob.ProductionPointsLeft == 0)
                    {
                        BatchJobShipComplete(colony, syconstruction, batchJob, designInfo);
                    }

                    if (pointsToUse == 0)
                        break;
                }
            }
        }

        static void BatchJobShipComplete(Entity constructingEntity, ShipYardAbilityDB shipYard, ShipYardJob batchJob, ShipClass shipDesign)
        {
        }
        

    }
}