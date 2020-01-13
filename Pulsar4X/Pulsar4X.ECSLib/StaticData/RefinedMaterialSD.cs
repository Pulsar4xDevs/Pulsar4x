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