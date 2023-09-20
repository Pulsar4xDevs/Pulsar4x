using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    public class MineralBlueprint : SerializableGameData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
        public Dictionary<BodyType, double> Abundance { get; set; }
    }
}