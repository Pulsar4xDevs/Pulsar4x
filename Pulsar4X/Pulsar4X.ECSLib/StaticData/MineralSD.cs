using System;
using System.Collections.Generic;
using Pulsar4X.Modding;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public class MineralSD : SerializableGameData, ICargoable
    {
        public string Name { get; set; }
        public string Description;
        public Guid ID { get; set; }
        public Guid CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
        public Dictionary<BodyType, double> Abundance;
    }
}
