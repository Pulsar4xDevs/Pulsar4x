using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    public class IndustryAbilityDB : BaseDataBlob, IAbilityDescription
    {
        public class ProductionLine
        {
            public string Name;
            public double MaxVolume;
            public Dictionary<string, int> IndustryTypeRates = new ();
            public List<IndustryJob> Jobs = new ();
        }

        public Dictionary<string, ProductionLine> ProductionLines { get; } = new ();

        [JsonConstructor]
        private IndustryAbilityDB()
        {
        }

        public IndustryAbilityDB(Dictionary<string, ProductionLine> productionLines)
        {
            ProductionLines = productionLines;
        }
        public IndustryAbilityDB(string componentID, ProductionLine productionLine)
        {
            ProductionLines.Add(componentID, productionLine);
        }

        public IndustryAbilityDB(IndustryAbilityDB db)
        {
            ProductionLines = new Dictionary<string, ProductionLine>(db.ProductionLines);
        }

        public override object Clone()
        {
            return new IndustryAbilityDB(this);
        }


        public List<IConstructableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {
            return factionInfoDB.IndustryDesigns.Values.ToList();
        }

        public string AbilityName()
        {
            return "Production Industry";
        }

        public string AbilityDescription()
        {
            //string time = StaticRefLib.Game.Settings.EconomyCycleTime.ToString();
            string desc = "Refines and Constructs Materials and Items at Rates of: \n";
            foreach (var kvpLines in ProductionLines)
            {

                desc += kvpLines.Value.Name + "\n";
                // foreach (var kvpRates in kvpLines.Value.IndustryTypeRates)
                // {
                //     string industryName =  "   " + StaticRefLib.StaticData.IndustryTypes[kvpRates.Key].Name;
                //     desc += industryName + "\t" + kvpRates.Value + "\n";
                // }

            }

            return desc; //+ "per " + time;
        }
    }
}
