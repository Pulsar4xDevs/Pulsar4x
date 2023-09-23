using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Designs
{
    [JsonObject]
    public class ShipDesign : ICargoable, IConstructableDesign, ISerializable
    {
        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.CanBeLaunched;
        public string UniqueID { get; private set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string CargoTypeID { get; }
        public int DesignVersion { get; set; }= 0;
        public bool IsObsolete { get; set; } = false;
        public bool IsValid { get; set; } = true; // Used by ship designer & production
        public long MassPerUnit { get; private set; }
        public double VolumePerUnit { get; private set; }
        public double Density { get; }

        private string _factionGuid;

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

        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(designInfo.ResourceCosts);
            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;

            var faction = industryEntity.GetFactionOwner;

            var ship = ShipFactory.CreateShip((ShipDesign)designInfo, faction, industryEntity.GetSOIParentEntity());
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

        public ShipDesign(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (ArmorBlueprint armorType, float thickness) armor)
        {
            _factionGuid = faction.OwningEntity.Guid;
            faction.ShipDesigns.Add(UniqueID, this);
            faction.IndustryDesigns[UniqueID] = this;
            Initialise(faction.Data.CargoGoods, name, components, armor);
        }

        // Fixme: needs to use the FactionDataStore somehow
        // public ShipDesign(SerializationInfo info, StreamingContext context)
        // {
        //     if (info == null)
        //         throw new ArgumentNullException("info");

        //     UniqueID = (string)info.GetValue(nameof(UniqueID), typeof(string));
        //     _factionGuid = (string)info.GetValue(nameof(_factionGuid), typeof(string));
        //     var name = (string)info.GetValue(nameof(Name), typeof(string));
        //     var components = (List<(ComponentDesign design, int count)>)info.GetValue(nameof(Components), typeof(List<(ComponentDesign design, int count)>));
        //     var armor = ((ArmorBlueprint armorType, float thickness))info.GetValue(nameof(Armor), typeof((ArmorBlueprint armorType, float thickness)));

        //     Initialise(name, components, armor);
        // }

        public void Initialise(CargoDefinitionsLibrary cargoLibrary, string name, List<(ComponentDesign design, int count)> components, (ArmorBlueprint armorType, float thickness) armor)
        {
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
            var armorMass = GetArmorMass(DamageProfileDB, cargoLibrary);
            MassPerUnit += (long)Math.Round(armorMass);
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = (long)(MassPerUnit * 0.1);
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(UniqueID), UniqueID);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_factionGuid), _factionGuid);
            info.AddValue(nameof(Armor), Armor);
            info.AddValue(nameof(Components), Components);
        }
    }
}
