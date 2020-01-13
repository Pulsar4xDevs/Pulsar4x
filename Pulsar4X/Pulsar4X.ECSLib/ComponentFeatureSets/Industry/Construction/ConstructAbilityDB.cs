using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Pulsar4X.ECSLib.Industry
{

    public class ConstructJob : JobBase
    {
      
        public Guid IndustryType { get; internal set; }
        public Entity InstallOn { get; set; }

        public ConstructJob()
        {
        }

        public ConstructJob(FactionInfoDB factionInfo, Guid designGuid)
        {
            ItemGuid = designGuid;
            ComponentDesign design = factionInfo.ComponentDesigns[ItemGuid];
            Name = design.Name;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            ResourcesRequired = design.ResourceCosts;
            IndustryType = design.IndustryTypeID;
        }

        public ConstructJob(Guid designGuid, Guid industryType, ushort numberOrderd, int jobPoints, bool auto, 
                            Dictionary<Guid,int> resourceCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            IndustryType = industryType;
            ResourcesRequired = resourceCost;
        }

        public ConstructJob(ComponentDesign design, ushort numOrdered, bool auto): base(design.ID, numOrdered, design.IndustryPointCosts, auto)
        {
            Name = design.Name;
            ResourcesRequired = design.ResourceCosts;
            IndustryType = design.IndustryTypeID;
        }
        
        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }

    public class  ConstructAbilityDB : BaseDataBlob, IIndustryDB
    {
        public int ConstructionPoints { get; internal set; }

        [JsonProperty]
        public Dictionary<ConstructionType, int> ConstructionRates { get; internal set; }

        [JsonProperty]
        public List<JobBase> JobBatchList { get; internal set; }

        public List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {
            List<IConstrucableDesign> designs = new List<IConstrucableDesign>();
            foreach (var design in factionInfoDB.ComponentDesigns.Values)
            {
                designs.Add(design);
            }
            return designs;
        }


        public ConstructAbilityDB()
        {
            ConstructionRates = new Dictionary<ConstructionType, int>
            {
                {ConstructionType.Ordnance, 0}, 
                {ConstructionType.Installations, 0}, 
                {ConstructionType.Fighters, 0}, 
                {ConstructionType.ShipComponents, 0},
            };
            JobBatchList = new List<JobBase>();
        }

        public ConstructAbilityDB(ConstructAbilityDB db)
        {
            ConstructionRates = db.ConstructionRates;
            JobBatchList = db.JobBatchList;
        }

        public override object Clone()
        {
            return new ConstructAbilityDB(this);
        }
    }
}
