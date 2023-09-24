using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Newtonsoft.Json;

namespace Pulsar4X.Atb
{
    public class IndustryAtb : IComponentDesignAttribute
    {
        [JsonProperty]
        public Dictionary<string, int> IndustryPoints { get; private set; } = new ();

        [JsonProperty]
        private double MaxProductionVolume;

        public IndustryAtb(Dictionary<string, double> industryRates)
        {
            MaxProductionVolume = double.PositiveInfinity;

            int i = 0;
            foreach (var kvp in industryRates)
            {
                IndustryPoints[kvp.Key] = (int)kvp.Value;
                i++;
            }
        }

        public IndustryAtb(Dictionary<string, double> industryRates, double maxProductionVolume)
        {
            MaxProductionVolume = maxProductionVolume;

            int i = 0;
            foreach (var kvp in industryRates)
            {
                IndustryPoints[kvp.Key] = (int)kvp.Value;
                i++;
            }
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var db = parentEntity.GetDataBlob<IndustryAbilityDB>();
            IndustryAbilityDB.ProductionLine newline = new();
            newline.MaxVolume = MaxProductionVolume;
            newline.IndustryTypeRates = IndustryPoints;
            newline.Name = componentInstance.Name;

            if (db == null)
            {
                db = new IndustryAbilityDB(componentInstance.UniqueID, newline);
                parentEntity.SetDataBlob(db);
            }
            else
            {
                db.ProductionLines.Add(componentInstance.UniqueID, newline);
            }
        }

        public string AtbName()
        {
            return "Industry";
        }

        public string AtbDescription()
        {
            string industryTypesAndPoints = "";
            // foreach (var kvp in IndustryPoints)
            // {
            //     var name =StaticRefLib.StaticData.IndustryTypes[kvp.Key].Name;
            //     var amount = kvp.Value;

            //     industryTypesAndPoints += name + "\t" + amount + "\n";
            // }
            return industryTypesAndPoints;
        }

    }
}