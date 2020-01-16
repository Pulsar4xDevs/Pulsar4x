using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public class ProcessedMaterialSD : ICargoable, IConstrucableDesign
    {
        public string Name { get; set; }
        public Dictionary<Guid, int> ResourceCosts { get; } = new Dictionary<Guid, int>();
        public int IndustryPointCosts { get; set; }
        public Guid IndustryTypeID { get; set; }
        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, IndustryJob batchJob, IConstrucableDesign designInfo)
        {
            ProcessedMaterialSD material = (ProcessedMaterialSD)designInfo;
            StorageSpaceProcessor.AddCargo(storage, material, material.OutputAmount); //and add the product to the stockpile                        
            batchJob.ProductionPointsLeft = material.IndustryPointCosts; //and reset the points left for the next job in the batch.
        }



        public string Description;
        public Guid ID { get; set; }

        public Dictionary<Guid, int> MineralsRequired;
        public Dictionary<Guid, int> MaterialsRequired;

        public ushort WealthCost;
        public ushort OutputAmount;
        public Guid CargoTypeID { get; set; }
        public int Mass { get; set; }
    }
}