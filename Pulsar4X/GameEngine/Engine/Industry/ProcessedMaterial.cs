using System;
using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine.Industry
{
    public class ProcessedMaterial : ProcessedMaterialBlueprint, ICargoable, IConstructableDesign
    {
        public bool IsValid { get; set; } = true;

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
            MineralsRequired = blueprint.MineralsRequired;
            MaterialsRequired = blueprint.MaterialsRequired;
            WealthCost = blueprint.WealthCost;
            OutputAmount = blueprint.OutputAmount;
            CargoTypeID = blueprint.CargoTypeID;
            MassPerUnit = blueprint.MassPerUnit;
            VolumePerUnit = blueprint.VolumePerUnit;
        }

        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
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