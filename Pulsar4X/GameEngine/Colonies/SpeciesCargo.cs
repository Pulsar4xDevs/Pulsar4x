using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// One species of people, as freight. The cargo id is the species id, so a hold of
    /// passengers keeps track of which species they are. Colonists are not trade goods:
    /// they are added to a colony's <see cref="ColonyInfoDB.Population"/> and nothing else,
    /// so they never feed industry and never sit in a warehouse.
    /// </summary>
    public class SpeciesCargo : ICargoable
    {
        public const string CargoType = "passenger-storage";

        /// <summary>Average mass of one person, kg.</summary>
        public const long MassPerPerson = 100;

        /// <summary>Volume of one person, m^3.</summary>
        public const double VolumePerPerson = 0.065;

        public int ID { get; private set; } = Game.GetEntityID();

        public string UniqueID => SpeciesId.ToString();

        public string CargoTypeID => CargoType;

        public string Name { get; private set; } = "";

        /// <summary>The species entity this cargo belongs to.</summary>
        [JsonProperty]
        public int SpeciesId { get; private set; }

        public long MassPerUnit => MassPerPerson;

        public double VolumePerUnit => VolumePerPerson;

        [JsonConstructor]
        public SpeciesCargo()
        {
        }

        public SpeciesCargo(Entity species)
        {
            SpeciesId = species.Id;
            if (species.TryGetDataBlob<Pulsar4X.Names.NameDB>(out var name))
                Name = name.DefaultName;
        }

        public static bool IsColonist(ICargoable cargo)
        {
            return cargo is SpeciesCargo;
        }

        /// <summary>
        /// Every species a faction knows, as passenger cargo, so a ship's hold, a market listing and
        /// an order can resolve a person by id. They go into the ordinary cargo definitions, not into
        /// a production recipe: a species is not a material, so no industry line can consume one.
        /// Idempotent; an id already there is left alone.
        /// </summary>
        public static void Register(FactionDataStore data, IEnumerable<Entity> species)
        {
            if (data == null || species == null)
                return;
            foreach (var entity in species)
            {
                if (entity == null || !entity.IsValid || !entity.HasDataBlob<SpeciesDB>())
                    continue;
                var cargo = new SpeciesCargo(entity);
                if (!data.LockedCargoGoods.Contains(cargo.UniqueID) && !data.CargoGoods.Contains(cargo.UniqueID))
                    data.CargoGoods.Add(cargo);
            }
        }
    }
}
