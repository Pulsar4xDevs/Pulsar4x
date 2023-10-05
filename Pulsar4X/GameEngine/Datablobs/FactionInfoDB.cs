using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Events;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;

namespace Pulsar4X.Datablobs
{
    public class FactionInfoDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        public FactionDataStore Data { get; internal set; } = new FactionDataStore();

        [JsonProperty]
        public List<Entity> Species { get; internal set; } = new ();


        [JsonProperty]
        public List<string> KnownSystems { get; internal set; } = new ();


        public ReadOnlyDictionary<string, List<Entity>> KnownJumpPoints => new (InternalKnownJumpPoints);
        [JsonProperty]
        internal Dictionary<string, List<Entity>> InternalKnownJumpPoints = new ();


        [JsonProperty]
        public List<Entity> KnownFactions { get; internal set; } = new ();


        [PublicAPI]
        [JsonProperty]
        public List<Entity> Colonies { get; internal set; } = new ();

        [JsonProperty]
        public SafeList<int> Commanders { get; internal set; } = new ();

        [JsonProperty]
        public Dictionary<string, ShipDesign> ShipDesigns = new ();

        [JsonProperty]
        public Dictionary<string, OrdnanceDesign> MissileDesigns = new ();

        /// <summary>
        /// This includes non researched and not constructible designs.
        /// Does Not Include Refined Materials
        /// </summary>
        public ReadOnlyDictionary<string, ComponentDesign> ComponentDesigns => new (InternalComponentDesigns);
        [JsonProperty]
        internal Dictionary<string, ComponentDesign> InternalComponentDesigns = new ();


        /// <summary>
        /// this shoudl only be designs we can construct.
        /// Does Include Refined Materials.
        /// </summary>
        public Dictionary<string, IConstructableDesign> IndustryDesigns = new ();



        [JsonProperty]
        /// <summary>
        /// stores sensor contacts for the entire faction, when a contact is created it gets added here.
        /// </summary>
        internal Dictionary<int, SensorContact> SensorContacts = new ();

        public Dictionary<EventType, bool> HaltsOnEvent { get; } = new ();

        [JsonProperty]
        private Dictionary<Entity, uint> FactionAccessRoles { get; set; } = new ();
        internal ReadOnlyDictionary<Entity, AccessRole> AccessRoles => new (FactionAccessRoles.ToDictionary(kvp => kvp.Key, kvp => (AccessRole)kvp.Value));



        public FactionInfoDB()
        {
            var componentDesigns = new Dictionary<string, ComponentDesign>();
            var shipClasses = new Dictionary<string, ShipDesign>();
            SetIndustryDesigns(componentDesigns, shipClasses);
            HaltsOnEvent.Add(EventType.OrdersHalt, true);
        }

        public FactionInfoDB(
            FactionDataStore factionDataStore,
            List<Entity> species,
            List<string> knownSystems,
            List<Entity> colonies,
            Dictionary<string, ComponentDesign> componentDesigns,
            Dictionary<string, ShipDesign> shipClasses)
        {
            Data = factionDataStore;
            Species = species;
            KnownSystems = knownSystems;
            Colonies = colonies;
            InternalComponentDesigns = componentDesigns;
            ShipDesigns = shipClasses;
            KnownFactions = new List<Entity>();
            SetIndustryDesigns(componentDesigns, shipClasses);
            HaltsOnEvent.Add(EventType.OrdersHalt, true);
        }


        public FactionInfoDB(FactionInfoDB factionDB)
        {
            Data = factionDB.Data;
            Species = new List<Entity>(factionDB.Species);
            KnownSystems = new List<string>(factionDB.KnownSystems);
            KnownFactions = new List<Entity>(factionDB.KnownFactions);
            Colonies = new List<Entity>(factionDB.Colonies);
            InternalKnownJumpPoints = new Dictionary<string, List<Entity>>(factionDB.KnownJumpPoints);

            ShipDesigns = new Dictionary<string, ShipDesign>(factionDB.ShipDesigns);
            InternalComponentDesigns = new Dictionary<string, ComponentDesign>(factionDB.ComponentDesigns);
            IndustryDesigns = new Dictionary<string, IConstructableDesign>(factionDB.IndustryDesigns);
            HaltsOnEvent.Add(EventType.OrdersHalt, true);

        }

        public override object Clone()
        {
            return new FactionInfoDB(this);
        }

        void SetIndustryDesigns(
            Dictionary<string, ComponentDesign> componentDesigns,
            Dictionary<string, ShipDesign> shipClasses)
        {
            foreach (var mat in Data.CargoGoods.GetMaterialsList())
            {
                IndustryDesigns[mat.UniqueID] = mat;
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
            //((Game)context.Context).PostLoad += (sender, args) => { };
        }

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in Species)
            {
                hash = ObjectExtensions.ValueHash(item.Id, hash);
            }
            foreach (var item in KnownSystems)
            {
                hash = ObjectExtensions.ValueHash(item, hash);
            }
            foreach (var item in KnownFactions)
            {
                hash = ObjectExtensions.ValueHash(item.Id, hash);
            }
            foreach (var item in Colonies)
            {
                hash = ObjectExtensions.ValueHash(item.Id, hash);
            }
            foreach (var item in ShipDesigns.Keys)
            {
                hash = ObjectExtensions.ValueHash(item, hash);
            }
            foreach (var item in InternalComponentDesigns)
            {
                hash = ObjectExtensions.ValueHash(item.Key, hash);
                hash = ObjectExtensions.ValueHash(item.Value.UniqueID, hash);
            }
            foreach (var system in InternalKnownJumpPoints)
            {
                hash = ObjectExtensions.ValueHash(system.Key, hash);
                foreach (var jp in system.Value)
                {
                    hash = ObjectExtensions.ValueHash(jp.Id, hash);
                }

            }

            return hash;
        }
    }
}