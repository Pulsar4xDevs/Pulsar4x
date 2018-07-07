using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class FactionInfoDB : BaseDataBlob, IGetValuesHash
    {

        [JsonProperty]
        public List<Entity> Species { get; internal set; }


        [JsonProperty]
        public List<Guid> KnownSystems { get; internal set; }


        public ReadOnlyDictionary<Guid, List<Entity>> KnownJumpPoints => new ReadOnlyDictionary<Guid, List<Entity>>(InternalKnownJumpPoints);
        [JsonProperty]
        internal Dictionary<Guid, List<Entity>> InternalKnownJumpPoints = new Dictionary<Guid, List<Entity>>();


        [JsonProperty]
        public List<Entity> KnownFactions { get; internal set; }


        [PublicAPI]
        [JsonProperty]
        public List<Entity> Colonies { get; internal set; }

        [JsonProperty]
        public List<Entity> ShipClasses { get; internal set; }


        public ReadOnlyDictionary<Guid, Entity> ComponentDesigns => new ReadOnlyDictionary<Guid, Entity>(InternalComponentDesigns);
        [JsonProperty]
        internal Dictionary<Guid, Entity> InternalComponentDesigns = new Dictionary<Guid, Entity>();


        [PublicAPI]
        public ReadOnlyDictionary<Guid, Entity> MissileDesigns => new ReadOnlyDictionary<Guid, Entity>(InternalMissileDesigns);
        [JsonProperty]
        internal Dictionary<Guid, Entity> InternalMissileDesigns = new Dictionary<Guid, Entity>();

        [JsonProperty]
        /// <summary>
        /// stores sensor contacts for the entire faction, when a contact is created it gets added here. 
        /// </summary>
        internal Dictionary<Guid, Entity> SensorEntites = new Dictionary<Guid, Entity>();


        public FactionInfoDB() : this(new List<Entity>(), new List<Guid>(), new List<Entity>(), new List<Entity>() ) { }

        public FactionInfoDB(
            List<Entity> species,
            List<Guid> knownSystems,
            List<Entity> colonies,
            List<Entity> shipClasses)
        {
            Species = species;
            KnownSystems = knownSystems;
            Colonies = colonies;
            ShipClasses = shipClasses;
            KnownFactions = new List<Entity>();
            InternalComponentDesigns = new Dictionary<Guid, Entity>();
        }
        

        public FactionInfoDB(FactionInfoDB factionDB)
        {
            Species = new List<Entity>(factionDB.Species);
            KnownSystems = new List<Guid>(factionDB.KnownSystems);
            KnownFactions = new List<Entity>(factionDB.KnownFactions);
            Colonies = new List<Entity>(factionDB.Colonies);
            ShipClasses = new List<Entity>(factionDB.ShipClasses);
            InternalComponentDesigns = new Dictionary<Guid, Entity>(factionDB.ComponentDesigns);
            InternalMissileDesigns = new Dictionary<Guid, Entity>(factionDB.MissileDesigns);
            InternalKnownJumpPoints = new Dictionary<Guid, List<Entity>>(factionDB.KnownJumpPoints);
        }

        public override object Clone()
        {
            return new FactionInfoDB(this);
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            ((Game)context.Context).PostLoad += (sender, args) => { };
        }

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in Species)
            {
                hash = Misc.ValueHash(item.Guid, hash);
            }
            foreach (var item in KnownSystems)
            {
                hash = Misc.ValueHash(item, hash);
            }
            foreach (var item in KnownFactions)
            {
                hash = Misc.ValueHash(item.Guid, hash);
            }
            foreach (var item in Colonies)
            {
                hash = Misc.ValueHash(item.Guid, hash);
            }
            foreach (var item in ShipClasses)
            {
                hash = Misc.ValueHash(item.Guid, hash);
            }
            foreach (var item in InternalComponentDesigns)
            {
                hash = Misc.ValueHash(item.Key, hash);
                hash = Misc.ValueHash(item.Value.Guid, hash);
            }
            foreach (var item in InternalMissileDesigns)
            {
                hash = Misc.ValueHash(item.Key, hash);
                hash = Misc.ValueHash(item.Value.Guid, hash);
            }
            foreach (var system in InternalKnownJumpPoints)
            {
                hash = Misc.ValueHash(system.Key, hash);
                foreach (var jp in system.Value)
                {
                    hash = Misc.ValueHash(jp.Guid, hash);
                }

            }

            return hash;
        }
    }
}