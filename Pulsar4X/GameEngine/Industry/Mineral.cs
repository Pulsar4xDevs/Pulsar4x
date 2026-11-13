using Pulsar4X.Blueprints;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry
{
    public class Mineral : MineralBlueprint, ICargoable
    {
        public int ID { get; private set; } = Game.GetEntityID();

        public Mineral() {}

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
            ExtractionEnergy = blueprint.ExtractionEnergy;
        }
    }
}