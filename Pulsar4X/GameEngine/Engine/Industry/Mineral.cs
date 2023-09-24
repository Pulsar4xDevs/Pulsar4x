using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine.Industry
{
    public class Mineral : MineralBlueprint, ICargoable
    {
        public Mineral(MineralBlueprint blueprint)
        {
            FullIdentifier = blueprint.FullIdentifier;
            UniqueID = blueprint.UniqueID;
            Name = blueprint.Name;
            Description = blueprint.Description;
            CargoTypeID = blueprint.CargoTypeID;
            MassPerUnit = blueprint.MassPerUnit;
            VolumePerUnit = blueprint.VolumePerUnit;
            Abundance = blueprint.Abundance;
        }
    }
}