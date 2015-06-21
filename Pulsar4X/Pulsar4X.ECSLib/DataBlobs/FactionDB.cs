using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FactionDB : BaseDataBlob
    {
        [PublicAPI]
        public List<Entity> Species
        {
            get { return _species; }
            internal set { _species = value; }
        }
        [JsonProperty]
        private List<Entity> _species;

        [PublicAPI]
        public List<StarSystem> KnownSystems
        {
            get { return _knownSystems; }
            internal set { _knownSystems = value; }
        }
        [JsonProperty]
        private List<StarSystem> _knownSystems;

        [PublicAPI]
        public List<Entity> Colonies
        {
            get { return _colonies; }
            internal set { _colonies = value; }
        }
        [JsonProperty]
        private List<Entity> _colonies;

        [PublicAPI]
        public List<Entity> ShipClasses
        {
            get { return _shipClasses; }
            internal set { _shipClasses = value; }
        }
        [JsonProperty]
        private List<Entity> _shipClasses;

        public FactionDB()
            : this(new List<Entity>(), new List<StarSystem>(), new List<Entity>(), new List<Entity>() )
        {

        }

        public FactionDB(
            List<Entity> species,
            List<StarSystem> knownSystems,
            List<Entity> colonies,
            List<Entity> shipClasses)
        {
            Species = species;
            KnownSystems = knownSystems;
            Colonies = colonies;
            ShipClasses = shipClasses;
        }
        

        public FactionDB(FactionDB factionDB)
        {
            Species = new List<Entity>(factionDB.Species);
            KnownSystems = new List<StarSystem>(factionDB.KnownSystems);
            Colonies = new List<Entity>(factionDB.Colonies);
            ShipClasses = new List<Entity>(factionDB.ShipClasses);
        }

        public override object Clone()
        {
            return new FactionDB(this);
        }
    }
}