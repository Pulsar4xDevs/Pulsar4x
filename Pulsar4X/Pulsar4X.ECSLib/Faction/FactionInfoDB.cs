using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    public class FactionInfoDB : BaseDataBlob, IGetValuesHash
    {

        [JsonProperty]
        public List<Entity> Species { get; internal set; } = new List<Entity>();


        [JsonProperty]
        public List<Guid> KnownSystems { get; internal set; } = new List<Guid>();


        public ReadOnlyDictionary<Guid, List<Entity>> KnownJumpPoints => new ReadOnlyDictionary<Guid, List<Entity>>(InternalKnownJumpPoints);
        [JsonProperty]
        internal Dictionary<Guid, List<Entity>> InternalKnownJumpPoints = new Dictionary<Guid, List<Entity>>();


        [JsonProperty]
        public List<Entity> KnownFactions { get; internal set; } = new List<Entity>();


        [PublicAPI]
        [JsonProperty]
        public List<Entity> Colonies { get; internal set; } = new List<Entity>();

        [JsonProperty]
        public Dictionary<Guid, ShipDesign> ShipDesigns = new Dictionary<Guid, ShipDesign>();

        public ReadOnlyDictionary<Guid, ComponentDesign> ComponentDesigns => new ReadOnlyDictionary<Guid, ComponentDesign>(InternalComponentDesigns);
        [JsonProperty]
        internal Dictionary<Guid, ComponentDesign> InternalComponentDesigns = new Dictionary<Guid, ComponentDesign>();


        public Dictionary<Guid, IConstrucableDesign> IndustryDesigns = new Dictionary<Guid, IConstrucableDesign>();
        
        
        
        [PublicAPI]
        public ReadOnlyDictionary<Guid, Entity> MissileDesigns => new ReadOnlyDictionary<Guid, Entity>(InternalMissileDesigns);
        [JsonProperty]
        internal Dictionary<Guid, Entity> InternalMissileDesigns = new Dictionary<Guid, Entity>();

        [JsonProperty]
        /// <summary>
        /// stores sensor contacts for the entire faction, when a contact is created it gets added here. 
        /// </summary>
        internal Dictionary<Guid, SensorContact> SensorContacts = new Dictionary<Guid, SensorContact>();


        public FactionInfoDB()
        {
            Dictionary<Guid, ComponentDesign> componentDesigns = new Dictionary<Guid, ComponentDesign>();
            Dictionary<Guid, ShipDesign> shipClasses = new Dictionary<Guid, ShipDesign>();
            SetIndustryDesigns(componentDesigns, shipClasses);
        }

        public FactionInfoDB(
            List<Entity> species,
            List<Guid> knownSystems,
            List<Entity> colonies,
            Dictionary<Guid, ComponentDesign> componentDesigns,
            Dictionary<Guid, ShipDesign> shipClasses)
        {
            Species = species;
            KnownSystems = knownSystems;
            Colonies = colonies;
            InternalComponentDesigns = componentDesigns;
            ShipDesigns = shipClasses;
            KnownFactions = new List<Entity>();
            SetIndustryDesigns(componentDesigns, shipClasses);
        }
        

        public FactionInfoDB(FactionInfoDB factionDB)
        {
            Species = new List<Entity>(factionDB.Species);
            KnownSystems = new List<Guid>(factionDB.KnownSystems);
            KnownFactions = new List<Entity>(factionDB.KnownFactions);
            Colonies = new List<Entity>(factionDB.Colonies);
            InternalKnownJumpPoints = new Dictionary<Guid, List<Entity>>(factionDB.KnownJumpPoints);
            
            ShipDesigns = new Dictionary<Guid, ShipDesign>(factionDB.ShipDesigns);
            InternalComponentDesigns = new Dictionary<Guid, ComponentDesign>(factionDB.ComponentDesigns);
            InternalMissileDesigns = new Dictionary<Guid, Entity>(factionDB.MissileDesigns);
            IndustryDesigns = new Dictionary<Guid, IConstrucableDesign>(factionDB.IndustryDesigns);
            
        }

        public override object Clone()
        {
            return new FactionInfoDB(this);
        }

        void SetIndustryDesigns(            
            Dictionary<Guid, ComponentDesign> componentDesigns,
            Dictionary<Guid, ShipDesign> shipClasses)
        {
            foreach (var mat in StaticRefLib.StaticData.CargoGoods.GetMaterialsList())
            {
                IndustryDesigns[mat.ID] = mat;
            }
            foreach (var design in componentDesigns)
            {
                IndustryDesigns[design.Key] = design.Value;
            }
            foreach (var design in shipClasses)
            {
                IndustryDesigns[design.Key] = design.Value;
            }
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
            foreach (var item in ShipDesigns.Keys)
            {
                hash = Misc.ValueHash(item, hash);
            }
            foreach (var item in InternalComponentDesigns)
            {
                hash = Misc.ValueHash(item.Key, hash);
                hash = Misc.ValueHash(item.Value.ID, hash);
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