using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Blueprints
{
    public class MineralBlueprint : Blueprint
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
        public Dictionary<BodyType, double> Abundance { get; set; }
    }
}