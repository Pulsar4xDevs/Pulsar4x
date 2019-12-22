using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Pulsar4X.ECSLib.Industry
{

    public class ConstructJob : JobBase
    {
      
        public ConstructionType ConstructionType { get; internal set; }
        public Entity InstallOn { get; set; }
        public Dictionary<Guid, int> MineralsRequired { get; internal set; }
        public Dictionary<Guid, int> MaterialsRequired { get; internal set; }
        public Dictionary<Guid, int> ComponentsRequired { get; internal set; }

        public ConstructJob()
        {
        }

        public ConstructJob(FactionInfoDB factionInfo, Guid designGuid)
        {
            ItemGuid = designGuid;
            ComponentDesign design = factionInfo.ComponentDesigns[ItemGuid];
            Name = design.Name;
            MineralsRequired = design.MineralCosts;
            MaterialsRequired = design.MaterialCosts;
            ComponentsRequired = design.ComponentCosts;
            ProductionPointsLeft = design.BuildPointCost;
            ProductionPointsCost = design.BuildPointCost;
            ConstructionType = design.ConstructionType;
        }

        public ConstructJob(Guid designGuid, ConstructionType constructionType, ushort numberOrderd, int jobPoints, bool auto, 
                            Dictionary<Guid,int> mineralCost, Dictionary<Guid, int> matCost, Dictionary<Guid,int> componentCost  ): 
            base(designGuid, numberOrderd, jobPoints, auto)
        {
            ConstructionType = constructionType;
            MineralsRequired = new Dictionary<Guid, int>(mineralCost);
            MaterialsRequired = new Dictionary<Guid, int>(matCost);
            ComponentsRequired = new Dictionary<Guid, int>(componentCost);
        }

        public ConstructJob(ComponentDesign design, ushort numOrdered, bool auto): base(design.ID, numOrdered, design.BuildPointCost, auto)
        {
            Name = design.Name;
            ConstructionType = design.ConstructionType;
            MineralsRequired = design.MineralCosts;
            MaterialsRequired = design.MaterialCosts;
            ComponentsRequired = design.ComponentCosts;
            ConstructionType = design.ConstructionType;
        }
        
        public override void InitialiseJob(FactionInfoDB factionInfo, Entity industryEntity, Guid guid, ushort numberOrderd, bool auto)
        {
            ItemGuid = guid;
            ComponentDesign design = factionInfo.ComponentDesigns[ItemGuid];
            Name = design.Name;
            MineralsRequired = design.MineralCosts;
            MaterialsRequired = design.MaterialCosts;
            ComponentsRequired = design.ComponentCosts;
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            ProductionPointsLeft = design.BuildPointCost;
            ProductionPointsCost = design.BuildPointCost;
            ConstructionType = design.ConstructionType;
            Auto = auto;
            
        }
    }

    public class  ConstructAbilityDB : BaseDataBlob, IIndustryDB
    {
        public int PointsPerTick { get; internal set; }

        [JsonProperty]
        public Dictionary<ConstructionType, int> ConstructionRates { get; internal set; }

        [JsonProperty]
        public List<JobBase> JobBatchList { get; internal set; }

        public List<ICargoable> GetJobItems(FactionInfoDB factionInfoDB)
        {
            List<ICargoable> designs = new List<ICargoable>();
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
                {ConstructionType.Ships, 0},
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
