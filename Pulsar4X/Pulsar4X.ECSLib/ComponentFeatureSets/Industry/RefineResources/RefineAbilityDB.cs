using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib.Industry
{
    public class RefineingJob : JobBase
    {
        
        public Dictionary<Guid, int> MineralsRequired { get; internal set; }
        public Dictionary<Guid, int> MaterialsRequired { get; internal set; }

        public RefineingJob()
        {
        }
        
        public RefineingJob(FactionInfoDB factionInfo, Guid materialID)
        {
            ItemGuid = materialID;
            var design = StaticRefLib.StaticData.CargoGoods.GetMaterial(materialID);
            Name = design.Name;
            MineralsRequired = design.MineralsRequired;
            MaterialsRequired = design.MaterialsRequired;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            design.MineralsRequired?.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            design.MaterialsRequired?.ToList().ForEach(x => ResourcesRequired[x.Key] = x.Value);
            NumberOrdered = 1;
        }

        public RefineingJob(Guid matGuid, ushort numberOrderd, int jobPoints, bool auto): base(matGuid, numberOrderd, jobPoints, auto)
        {
            Name = StaticRefLib.StaticData.CargoGoods.GetMaterial(matGuid).Name;
        }
        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }

    public class RefineAbilityDB : BaseDataBlob, ICreateViewmodel, IIndustryDB
    {
        public int ConstructionPoints { get; internal set; }

        //recalc this on game load todo implement this in the processor. 
        public Dictionary<Guid, int> RefiningRates{ get; internal set; }

        [JsonProperty] 
        public List<JobBase> JobBatchList { get; internal set; } = new List<JobBase>();
        public List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {
            var mats = StaticRefLib.StaticData.CargoGoods.GetMaterialsList();
            List<IConstrucableDesign> refinables = new List<IConstrucableDesign>();
            foreach (var mat in mats)
            {
                refinables.Add(mat);
            }
            return refinables;
        }


        public RefineAbilityDB()
        {
            RefiningRates = new Dictionary<Guid, int>();
        }

        public RefineAbilityDB(RefineAbilityDB db)
        {
            RefiningRates = new Dictionary<Guid, int>(db.RefiningRates);
            JobBatchList = new List<JobBase>(db.JobBatchList);
        }

        public override object Clone()
        {
            return new RefineAbilityDB(this);
        }

        public IDBViewmodel CreateVM(Game game, CommandReferences cmdRef)
        {
            return new RefiningVM(game, cmdRef, this);
        }
    }
}