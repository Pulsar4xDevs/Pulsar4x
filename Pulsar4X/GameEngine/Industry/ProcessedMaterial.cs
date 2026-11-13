using System;
using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry
{
    public class ProcessedMaterial : ProcessedMaterialBlueprint, ICargoable, IConstructableDesign
    {
        public int ID { get; private set; } = Game.GetEntityID();
        public bool IsValid { get; set; } = true;

        public ProcessedMaterial() { }

        public ProcessedMaterial(ProcessedMaterialBlueprint blueprint)
        {
            FullIdentifier = blueprint.FullIdentifier;
            UniqueID = blueprint.UniqueID;
            Name = blueprint.Name;
            Formulas = blueprint.Formulas;
            ResourceCosts = blueprint.ResourceCosts;
            IndustryPointCosts = blueprint.IndustryPointCosts;
            IndustryTypeID = blueprint.IndustryTypeID;
            Description = blueprint.Description;
            GuiHints = blueprint.GuiHints;
            WealthCost = blueprint.WealthCost;
            OutputAmount = blueprint.OutputAmount;
            CargoTypeID = blueprint.CargoTypeID;
            MassPerUnit = blueprint.MassPerUnit;
            VolumePerUnit = blueprint.VolumePerUnit;
            EnergyCost = blueprint.EnergyCost;
        }

        /// <summary>Default batch energy, kWh, when the recipe sets no EnergyCost (IP x EnergyPerPoint).</summary>
        [Newtonsoft.Json.JsonIgnore]
        internal double FallbackEnergyCost { get; set; }

        /// <summary>Refining energy of one batch, kWh: the recipe's EnergyCost, else the fallback.</summary>
        double IConstructableDesign.EnergyCost => BatchEnergy;

        /// <summary>Refining energy of one batch, kWh: the recipe's EnergyCost, else the fallback.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public double BatchEnergy => EnergyCost is double e && double.IsFinite(e) ? Math.Max(0, e) : FallbackEnergyCost;

        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            ProcessedMaterial material = (ProcessedMaterial)designInfo;
            storage.AddCargoByUnit(material, (long)OutputAmount);

            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(material.ResourceCosts);
            batchJob.ProductionPointsLeft = batchJob.ProductionPointsCost; //and reset the points left for the next job in the batch.
            batchJob.NumberCompleted += 1;

            if (batchJob.NumberCompleted == batchJob.NumberOrdered)
            {
                industryDB.ProductionLines[productionLine].Jobs.Remove(batchJob);
                if (batchJob.Auto)
                {
                    batchJob.NumberCompleted = 0;
                    industryDB.ProductionLines[productionLine].Jobs.Add(batchJob);
                }
            }
        }
    }
}