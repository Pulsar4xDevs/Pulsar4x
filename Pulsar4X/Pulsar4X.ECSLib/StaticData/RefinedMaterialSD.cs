using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public struct RefinedMaterialSD : ICargoable
    {
        public string Name;
        public string Description;
        public Guid ID { get; set; }

        public Dictionary<Guid, int> RawMineralCosts;
        public Dictionary<Guid, int> RefinedMateraialsCosts;
        public ushort RefineryPointCost;
        public ushort WealthCost;
        public ushort OutputAmount;
        public Guid CargoTypeID { get; set; }
        public float Mass { get; set; }
    }
}