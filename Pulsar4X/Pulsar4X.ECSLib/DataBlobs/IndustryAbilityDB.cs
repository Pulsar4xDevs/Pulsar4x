using Newtonsoft.Json;
using Pulsar4X.ECSLib.Industry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public class IndustryAbilityDB : BaseDataBlob, IAbilityDescription
    {
        public class ProductionLine
        {
            public string Name;
            public double MaxVolume;
            public Dictionary<Guid, int> IndustryTypeRates = new Dictionary<Guid, int>();
            public List<IndustryJob> Jobs = new List<IndustryJob>();
        }

        //public int ConstructionPoints { get; } = 0;
        //public List<JobBase> JobBatchList { get; } = new List<JobBase>();

        //public Dictionary<Guid, int> IndustryTypeRates { get; } = new Dictionary<Guid, int>();
        //public Dictionary<Guid, List<JobBase>> JobsBytype = new Dictionary<Guid, List<JobBase>>();


        public Dictionary<Guid, ProductionLine> ProductionLines { get; } = new Dictionary<Guid, ProductionLine>();

        [JsonConstructor]
        private IndustryAbilityDB()
        {
        }

        public IndustryAbilityDB(Dictionary<Guid, ProductionLine> productionLines)
        {
            ProductionLines = productionLines;
        }
        public IndustryAbilityDB(Guid componentID, ProductionLine productionLine)
        {
            ProductionLines.Add(componentID, productionLine);
        }

        public IndustryAbilityDB(IndustryAbilityDB db)
        {
            //IndustryTypeRates = new Dictionary<Guid, int>(db.IndustryTypeRates);
            ProductionLines = new Dictionary<Guid, ProductionLine>(db.ProductionLines);
        }

        public override object Clone()
        {
            return new IndustryAbilityDB(this);
        }


        public List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {
            return factionInfoDB.IndustryDesigns.Values.ToList();
        }

        public string AbilityName()
        {
            return "Production Industry";
        }

        public string AbilityDescription()
        {
            string time = StaticRefLib.Game.Settings.EconomyCycleTime.ToString();
            string desc = "Refines and Constructs Materials and Items at Rates of: \n";
            foreach (var kvpLines in ProductionLines)
            {

                desc += kvpLines.Value.Name + "\n";
                foreach (var kvpRates in kvpLines.Value.IndustryTypeRates)
                {
                    string industryName =  "   " + StaticRefLib.StaticData.IndustryTypes[kvpRates.Key].Name;
                    desc += industryName + "\t" + kvpRates.Value + "\n";
                }

            }

            return desc + "per " + time;
        }
    }
}
