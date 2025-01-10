using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Damage;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Ships
{
    [JsonObject]
    public class ShipDesign : ICargoable, IConstructableDesign, ISerializable
    {
        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.CanBeLaunched;
        public int ID { get; private set; } = Game.GetEntityID();
        public string UniqueID { get; private set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string CargoTypeID { get; }
        public int DesignVersion { get; set; }= 0;
        public bool IsObsolete { get; set; } = false;
        public bool IsValid { get; set; } = true; // Used by ship designer & production
        public long MassPerUnit { get; private set; }
        public double VolumePerUnit { get; private set; }
        public double Density { get; }

        private int _factionId;

        /// <summary>
        /// m^3
        /// </summary>
        //public double Volume;

        /// <summary>
        /// This lists all the components in order for the design, from front to back, and how many "wide".
        /// note that component types can be split/arranged ie:
        /// (bridge,1), (fueltank,2), (cargo,1)(fueltank,1)(engine,3) would have a bridge at teh front,
        /// then two fueltanks behind, one cargo, another single fueltank, then finaly three engines.
        /// </summary>
        public List<(ComponentDesign design, int count)> Components;
        public (ArmorBlueprint type, float thickness) Armor;
        public Dictionary<string, long> ResourceCosts { get; internal set; } = new Dictionary<string, long>();
        public Dictionary<string, long> MineralCosts = new Dictionary<string, long>();
        public Dictionary<string, long> MaterialCosts = new Dictionary<string, long>();
        public Dictionary<string, long> ComponentCosts = new Dictionary<string, long>();
        public Dictionary<string, long> ShipInstanceCost = new Dictionary<string, long>();
        public int CrewReq;
        public long IndustryPointCosts { get; private set; }

        //TODO: this is one of those places where moddata has bled into hardcode...
        //the guid here is from IndustryTypeData.json "Ship Assembly"
        public string IndustryTypeID { get; } = "ship-assembly"; //new Guid("91823C5B-A71A-4364-A62C-489F0183EFB5");
        public ushort OutputAmount { get; } = 1;

        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(designInfo.ResourceCosts);
            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;

            var faction = industryEntity.GetFactionOwner;
            var industryParent = industryEntity.GetSOIParentEntity();

            if(industryParent == null) throw new NullReferenceException("industryParent cannot be null");

            var ship = ShipFactory.CreateShip((ShipDesign)designInfo, faction, industryParent);
            if(faction.TryGetDatablob<FleetDB>(out var fleetDB))
            {
                fleetDB.AddChild(ship);
            }

            if (batchJob.NumberCompleted == batchJob.NumberOrdered)
            {
                industryDB.ProductionLines[productionLine].Jobs.Remove(batchJob);
                if (batchJob.Auto)
                {
                    batchJob.NumberCompleted = 0;
                    industryDB.ProductionLines[productionLine].Jobs.Add(batchJob);
                }
            }
        }

        public int CreditCost;
        public EntityDamageProfileDB DamageProfileDB;

        [JsonConstructor]
        internal ShipDesign()
        {
        }

        public ShipDesign(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (ArmorBlueprint armorType, float thickness) armor, string? id = null)
        {
            if(id != null) UniqueID = id;
            _factionId = faction.OwningEntity.Id;
            Name = name;
            Components = components;
            Armor = armor;
            MassPerUnit = 0;
            foreach (var component in components)
            {
                MassPerUnit += component.design.MassPerUnit * component.count;
                CrewReq += component.design.CrewReq;
                CreditCost += component.design.CreditCost;
                VolumePerUnit += component.design.VolumePerUnit * component.count;
                if (ComponentCosts.ContainsKey(component.design.UniqueID))
                {
                    ComponentCosts[component.design.UniqueID] = ComponentCosts[component.design.UniqueID] + component.count;
                }
                else
                {
                    ComponentCosts.Add(component.design.UniqueID, component.count);
                }

            }
            DamageProfileDB = new EntityDamageProfileDB(components, armor);
            var armorMass = GetArmorMass(DamageProfileDB, faction.Data.CargoGoods);
            MassPerUnit += (long)Math.Round(armorMass);
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = (long)(MassPerUnit * 0.1);
        }
        
        /// <summary>
        /// this just stores the design in the factionInfo
        /// </summary>
        /// <param name="faction"></param>
        public void Initialise(FactionInfoDB faction)
        {
            faction.ShipDesigns[UniqueID] = this;
            faction.IndustryDesigns[UniqueID] = this;
        }

        public static double GetArmorMass(EntityDamageProfileDB damageProfile, CargoDefinitionsLibrary cargoLibrary)
        {
            if (damageProfile.ArmorVertex.Count == 0)
                return 0;
            var armor = damageProfile.Armor;
            double surfaceArea = 0;
            (int x, int y) v1 = damageProfile.ArmorVertex[0];
            for (int index = 1; index < damageProfile.ArmorVertex.Count; index++)
            {
                (int x, int y) v2 = damageProfile.ArmorVertex[index];

                var r1 = v1.y; //radius of top
                var r2 = v2.y; //radius of bottom
                var h = v2.x - v1.x; //height
                var c1 = 2* Math.PI * r1; //circumference of top
                var c2 = 2 * Math.PI * r2; //circumference of bottom
                var sl = Math.Sqrt(h * h + (r1 - r2) * (r1 - r2)); //slope of side

                surfaceArea += 0.5 * sl * (c1 + c2);

                v1 = v2;
            }

            var aresource = cargoLibrary.GetAny(armor.armorType.ResourceID);
            var amass = aresource.MassPerUnit;
            var avol = aresource.VolumePerUnit;
            var aden = amass / avol;
            var armorVolume = surfaceArea * armor.thickness * 0.001;
            var armorMass = armorVolume * aden;
            return armorMass;
        }

        /// <summary>
        /// Note: this itterates through the design list, so it's not particuarly efficent for per frame use.
        /// </summary>
        /// <param name="components">out list of components that have the given attribute</param>
        /// <typeparam name="T">attribute type</typeparam>
        /// <returns>true if design has componenst with this attribute</returns>
        public bool TryGetComponentsByAttribute<T>(out List<(ComponentDesign design, int count)> components)
            where T : IComponentDesignAttribute
        {
            bool hasComponents = false;
             components = new ();
            foreach (var component in Components)
            {
                if (component.design.HasAttribute<T>())
                {
                    hasComponents = true;
                    components.Add(component);
                }
            }
            return hasComponents;
        }

        /// <summary>
        /// Returns a dictionary of component designs by attribute type.
        /// Note that there will be doubleups of components where a component has multiple attributes.
        /// </summary>
        /// <returns></returns>
        public Dictionary<Type, List<(ComponentDesign design, int count)>> GetComponentsByAttribute()
        {
            Dictionary<Type, List<(ComponentDesign design, int count)>> dict = new();
            foreach (var component in Components)
            {
                foreach (var kvp in component.design.AttributesByType)
                {
                    if (!dict.ContainsKey(kvp.Key))
                        dict.Add(kvp.Key, new List<(ComponentDesign design, int count)>());
                    dict[kvp.Key].Add(component);
                }
            }
            return dict;
        }

        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(UniqueID), UniqueID);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_factionId), _factionId);
            info.AddValue(nameof(Armor), Armor);
            info.AddValue(nameof(Components), Components);
        }

        /// <summary>
        /// creates a clone of this object
        /// </summary>
        /// <returns></returns>
        public ShipDesign Clone(FactionInfoDB faction)
        {
            var components = new List<(ComponentDesign design, int count)>(Components);
            var armor = Armor;
            var newDesign = new ShipDesign(faction, Name, components, armor);
            
            return newDesign;
            
        }
    }
}
