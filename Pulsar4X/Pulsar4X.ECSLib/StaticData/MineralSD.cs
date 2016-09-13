using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public struct MineralSD : ICargoable
    {
        public string Name { get; set; }
        public string Description;
        public Guid ID { get; set; }
        public Guid CargoTypeID { get; set; }
        public float Mass { get; set; }
        public Dictionary<BodyType, double> Abundance;
    }
}
