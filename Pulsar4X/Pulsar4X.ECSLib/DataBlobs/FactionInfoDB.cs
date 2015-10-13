using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FactionInfoDB : BaseDataBlob
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
        public List<Entity> KnownFactions 
        {
            get {return _knownFactions;} 
            internal set { _knownFactions = value; }
        }
        [JsonProperty]
        private List<Entity> _knownFactions;


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

        [PublicAPI]
        [JsonProperty]
        public Dictionary<Guid, Entity> ComponentDesigns { get; internal set; }
        


        public FactionInfoDB()
            : this(new List<Entity>(), new List<StarSystem>(), new List<Entity>(), new List<Entity>() )
        {

        }

        public FactionInfoDB(
            List<Entity> species,
            List<StarSystem> knownSystems,
            List<Entity> colonies,
            List<Entity> shipClasses)
        {
            Species = species;
            KnownSystems = knownSystems;
            Colonies = colonies;
            ShipClasses = shipClasses;
            KnownFactions = new List<Entity>();
            ComponentDesigns = new JDictionary<Guid, Entity>();
        }
        

        public FactionInfoDB(FactionInfoDB factionDB)
        {
            Species = new List<Entity>(factionDB.Species);
            KnownSystems = new List<StarSystem>(factionDB.KnownSystems);
            KnownFactions = new List<Entity>(factionDB.KnownFactions);
            Colonies = new List<Entity>(factionDB.Colonies);
            ShipClasses = new List<Entity>(factionDB.ShipClasses);

        }

        public override object Clone()
        {
            return new FactionInfoDB(this);
        }
    }
}