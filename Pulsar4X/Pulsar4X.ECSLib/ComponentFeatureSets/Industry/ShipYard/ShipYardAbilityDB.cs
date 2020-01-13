using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;


namespace Pulsar4X.ECSLib.Industry
{
    public class ShipYardAbilityDB : BaseDataBlob, IIndustryDB
    {
        [JsonProperty]
        public int ConstructionPoints { get; internal set; }

        [JsonProperty]
        public List<JobBase> JobBatchList { get; } = new List<JobBase>();

        [JsonProperty]
        public List<(Guid componentID, int size)> Slips = new List<(Guid componentID, int size)>();
        


        public List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {

                List<IConstrucableDesign> designs = new List<IConstrucableDesign>();
                foreach (var design in factionInfoDB.ShipDesigns.Values)
                {
                    designs.Add(design);
                }
                return designs;
            
        }

        public ShipYardAbilityDB()
        {
        }

        public ShipYardAbilityDB(ShipYardAbilityDB db)
        {
            ConstructionPoints = db.ConstructionPoints;
            Slips = new List<(Guid componentID, int size)>(db.Slips);
            JobBatchList = new List<JobBase>(db.JobBatchList);
        }

        public override object Clone()
        {
            return new ShipYardAbilityDB(this);
        }
    }
    
    public class ShipYardJob : JobBase
    {
        public Guid SlipID;

        public ShipDesign ShipDesign { get; set; }

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

        public ShipYardJob(FactionInfoDB factionInfo, Guid designGuid)
        {
            ItemGuid = designGuid;
            ShipDesign = factionInfo.ShipDesigns[ItemGuid];
            Name = ShipDesign.Name;
            MineralsRequired = ShipDesign.MineralCosts;
            MaterialsRequired = ShipDesign.MaterialCosts;
            ComponentsRequired = ShipDesign.ComponentCosts;
            ProductionPointsLeft = ShipDesign.IndustryPointCosts;
            ProductionPointsCost = ShipDesign.IndustryPointCosts;
            ShipDesign.MineralCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            ShipDesign.MaterialCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            ShipDesign.ComponentCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            ShipDesignInstancesRequired = ShipDesign.ShipInstanceCost;
            
            NumberOrdered = 1;

        }
        
        public ShipYardJob(ShipDesign design, ushort numberOrderd, int jobPoints, bool auto, 
                           Dictionary<Guid,int> mineralCost, Dictionary<Guid, int> matCost, Dictionary<Guid,int> componentCost  ): 
            base(design.ID, numberOrderd, jobPoints, auto)
        {
            Name = design.Name;
            ShipDesign = design;
            MineralsRequired = new Dictionary<Guid, int>(mineralCost);
            MaterialsRequired = new Dictionary<Guid, int>(matCost);
            ComponentsRequired = new Dictionary<Guid, int>(componentCost);
            design.MineralCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            design.MaterialCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            design.ComponentCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
        }

        public ShipYardJob(ShipDesign design, ushort numOrdered, Guid slipID, bool auto): 
            base(design.ID, numOrdered, design.IndustryPointCosts, auto)
        {
            SlipID = slipID;
            Name = design.Name;
            ShipDesign = design;
            MineralsRequired = design.MineralCosts;
            MaterialsRequired = design.MaterialCosts;
            ComponentsRequired = design.ComponentCosts;
            design.MineralCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            design.MaterialCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            design.ComponentCosts.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            ShipDesignInstancesRequired = design.ShipInstanceCost;
        }

        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
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

            
            int maxPoints = syconstruction.ConstructionPoints; 

            List<ShipYardJob> constructionJobs = new List<ShipYardJob>(syconstruction.JobBatchList.OfType<ShipYardJob>());
            
            foreach (ShipYardJob batchJob in constructionJobs.ToArray())
            {
                var designInfo = batchJob.ShipDesign;
                
                //total number of resources requred for a single job in this batch
                int resourcePoints = designInfo.MineralCosts.Sum(item => item.Value);
                resourcePoints += designInfo.MaterialCosts.Sum(item => item.Value);
                resourcePoints += designInfo.ComponentCosts.Sum(item => item.Value);

                //how many construction points each resourcepoint is worth.
                float pointPerResource = (float)designInfo.IndustryPointCosts / resourcePoints;
                
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
                        BatchJobShipComplete(colony, batchJob, designInfo);
                    }

                    if (pointsToUse == 0)
                        break;
                }
            }
        }

        internal static void BatchJobShipComplete(Entity constructingEntity, ShipYardJob batchJob, ShipDesign shipDesign)
        {
            //need to sort out what we're going to do ui wise here.
            //we want to be able to:
            //store small ships in cargo, 
            //transfer small ships to another entity (carrier)
            //auto launch ships
            //leave them in the slip and launch them manualy.
            //have them auto join a taskforce (and automaticaly path to it)
            //launching from a planet should cost fuel and materials. 
        }

        internal static void ReCalcConstructionRate(Entity industryEntity)
        {

            ShipYardAbilityDB sy = industryEntity.GetDataBlob<ShipYardAbilityDB>();
            var instancesDB = industryEntity.GetDataBlob<ComponentInstancesDB>();
            List<(Guid ID, int size)> totalSlips = new List<(Guid ID, int size)>();
            int totalConstructionPoints = 0;
            if (instancesDB.TryGetComponentsByAttribute<ShipYardAtbDB>(out var instances))
            {
                foreach (var instance in instances)
                {
                    float healthPercent = instance.HealthPercent();
                    var designInfo = instance.Design.GetAttribute<ShipYardAtbDB>();
                    totalConstructionPoints += designInfo.ConstructionPoints;
                    totalSlips.Add((instance.ID, designInfo.SlipSize));
                }
            }
            
            sy.ConstructionPoints = totalConstructionPoints;
            sy.Slips = totalSlips;
        }
    }
    
    public class ShipYardAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public int ConstructionPoints;
        public int SlipSize;
        public ShipYardAtbDB(int constructionPoints, int slipSize)
        {
            ConstructionPoints = constructionPoints;
            SlipSize = slipSize;
        }

        [JsonConstructor]
        public ShipYardAtbDB()
        {
            
        }

        public override object Clone()
        {
            return new ShipYardAtbDB(this);
        }

        public ShipYardAtbDB(ShipYardAtbDB atb)
        {
            ConstructionPoints = atb.ConstructionPoints;
            SlipSize = atb.SlipSize;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<ShipYardAbilityDB>())
                parentEntity.SetDataBlob(new ShipYardAbilityDB());
            ShipyardProcessor.ReCalcConstructionRate(parentEntity);
        }
    }
}