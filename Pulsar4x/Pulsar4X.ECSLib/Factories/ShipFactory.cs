using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    [JsonObject]
    public class ShipDesign : ICargoable, IConstrucableDesign, ISerializable
    {
        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.CanBeLaunched;
        public Guid ID { get; private set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid CargoTypeID { get; }
        public int DesignVersion = 0;
        public bool IsObsolete = false;
        public long MassPerUnit { get; private set; }
        public double VolumePerUnit { get; private set; }
        public double Density { get; }

        private Guid _factionGuid;

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
        public (ArmorSD type, float thickness) Armor;
        public Dictionary<Guid, long> ResourceCosts { get; internal set; } = new Dictionary<Guid, long>();
        public Dictionary<Guid, long> MineralCosts = new Dictionary<Guid, long>();
        public Dictionary<Guid, long> MaterialCosts = new Dictionary<Guid, long>();
        public Dictionary<Guid, long> ComponentCosts = new Dictionary<Guid, long>();
        public Dictionary<Guid, long> ShipInstanceCost = new Dictionary<Guid, long>();
        public int CrewReq;
        public long IndustryPointCosts { get; private set; }
        
        //TODO: this is one of those places where moddata has bled into hardcode...
        //the guid here is from IndustryTypeData.json "Ship Assembly"
        public Guid IndustryTypeID { get; } = new Guid("91823C5B-A71A-4364-A62C-489F0183EFB5");

        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, Guid productionLine, IndustryJob batchJob, IConstrucableDesign designInfo)
        { 
            var industrydb = industryEntity.GetDataBlob<IndustryAbilityDB>();
        }

        public int CreditCost;
        public EntityDamageProfileDB DamageProfileDB;

        [JsonConstructor]
        internal ShipDesign()
        {
        }

        public ShipDesign(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (ArmorSD armorType, float thickness) armor)
        {
            _factionGuid = faction.OwningEntity.Guid;
            faction.ShipDesigns.Add(ID, this);
            faction.IndustryDesigns[ID] = this;
            Initialise(name, components, armor);
        }

        public ShipDesign(SerializationInfo info, StreamingContext context)
        {
            if (info == null) 
                throw new ArgumentNullException("info");

            ID = (Guid)info.GetValue(nameof(ID), typeof(Guid));
            _factionGuid = (Guid)info.GetValue(nameof(_factionGuid), typeof(Guid));
            var name = (string)info.GetValue(nameof(Name), typeof(string));
            var components = (List<(ComponentDesign design, int count)>)info.GetValue(nameof(Components), typeof(List<(ComponentDesign design, int count)>));
            var armor = ((ArmorSD armorType, float thickness))info.GetValue(nameof(Armor), typeof((ArmorSD armorType, float thickness)));

            Initialise(name, components, armor);
        }

        public void Initialise(string name, List<(ComponentDesign design, int count)> components, (ArmorSD armorType, float thickness) armor)
        {
            Name = name;
            Components = components;
            Armor = armor;
             
            foreach (var component in components)
            {
                MassPerUnit += component.design.MassPerUnit * component.count;
                CrewReq += component.design.CrewReq;
                CreditCost += component.design.CreditCost;
                VolumePerUnit += component.design.VolumePerUnit * component.count;
                if (ComponentCosts.ContainsKey(component.design.ID))
                {
                    ComponentCosts[component.design.ID] = ComponentCosts[component.design.ID] + component.count;
                }
                else
                {
                    ComponentCosts.Add(component.design.ID, component.count);
                }

            }
            var r = Math.Cbrt(VolumePerUnit * 3 / 4 / Math.PI);
            var s = 4 * Math.PI * r * r;
            var v = s * armor.thickness * 0.001; //armor thickness is in mm, volume is in m^3
            var m = v * armor.armorType.Density;
            MassPerUnit += (long)Math.Round(m);
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = MassPerUnit;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ID), ID);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_factionGuid), _factionGuid);
            info.AddValue(nameof(Armor), Armor);
            info.AddValue(nameof(Components), Components);
        }
    }

    public static class ShipFactory
    {
        /// <summary>
        /// new ship in a circular orbit at a distance of twice the parent bodies radius (size)
        /// </summary>
        /// <param name="shipDesign"></param>
        /// <param name="ownerFaction"></param>
        /// <param name="parent"></param>
        /// <param name="shipName"></param>
        /// <returns></returns>
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, string shipName = null)
        {
            
            double distanceFromParent = parent.GetDataBlob<MassVolumeDB>().RadiusInM * 2;
            var pos = new Vector3(distanceFromParent, 0, 0);
            var orbit = OrbitDB.FromPosition(parent, pos, shipDesign.MassPerUnit, parent.StarSysDateTime);
            return CreateShip(shipDesign, ownerFaction, orbit, parent, shipName);
        }

        /// <summary>
        /// new ship in a circular orbit at twice the parent bodies radius (size), and a given true anomaly
        /// </summary>
        /// <param name="shipDesign"></param>
        /// <param name="ownerFaction"></param>
        /// <param name="parent"></param>
        /// <param name="angleRad">true anomaly</param>
        /// <param name="shipName"></param>
        /// <returns></returns>
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, double angleRad, string shipName = null)
        {
            
            
            var distanceFromParent = parent.GetDataBlob<MassVolumeDB>().RadiusInM * 2;

            var x = distanceFromParent * Math.Cos(angleRad);
            var y = distanceFromParent * Math.Sin(angleRad);
            
            var pos = new Vector3( x,  y, 0);
            
            StarSystem starsys = (StarSystem)parent.Manager;
            var orbit = OrbitDB.FromPosition(parent, pos, shipDesign.MassPerUnit, parent.StarSysDateTime);
            return CreateShip(shipDesign, ownerFaction, orbit, parent, shipName);
        }

        /// <summary>
        /// new ship in a circular orbit at a given position from the parent. 
        /// </summary>
        /// <param name="shipDesign"></param>
        /// <param name="ownerFaction"></param>
        /// <param name="position"></param>
        /// <param name="parent"></param>
        /// <param name="shipName"></param>
        /// <returns></returns>
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Vector3 position, Entity parent, string shipName = null)
        {
            var orbit = OrbitDB.FromPosition(parent, position, shipDesign.MassPerUnit, parent.StarSysDateTime);
            return CreateShip(shipDesign, ownerFaction, orbit, parent, shipName);
        }

        /// <summary>
        /// new ship with an orbit and position defined by kepler elements.
        /// </summary>
        /// <param name="shipDesign"></param>
        /// <param name="ownerFaction"></param>
        /// <param name="ke"></param>
        /// <param name="parent"></param>
        /// <param name="shipName"></param>
        /// <returns></returns>
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction,  KeplerElements ke, Entity parent, string shipName = null)
        {
            OrbitDB orbit = OrbitDB.FromKeplerElements(parent,shipDesign.MassPerUnit, ke, parent.StarSysDateTime);
            var position = OrbitProcessor.GetPosition(ke, parent.StarSysDateTime);
            return CreateShip(shipDesign, ownerFaction, orbit, parent, shipName);
        }

        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, OrbitDB orbit,  Entity parent, string shipName = null)
        {

            var starsys = parent.Manager;
            var parentPosition = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            var position = parentPosition + OrbitProcessor.GetPosition(orbit.GetElements(), parent.StarSysDateTime);
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            
            var shipinfo = new ShipInfoDB(shipDesign);
            dataBlobs.Add(shipinfo);
            var mvdb = MassVolumeDB.NewFromMassAndVolume(shipDesign.MassPerUnit, shipDesign.VolumePerUnit);
            dataBlobs.Add(mvdb);
            PositionDB posdb = new PositionDB(position, starsys.ManagerGuid, parent);
            dataBlobs.Add(posdb);
            EntityDamageProfileDB damagedb = (EntityDamageProfileDB)shipDesign.DamageProfileDB.Clone(); 
            dataBlobs.Add(damagedb);
            ComponentInstancesDB compInstances = new ComponentInstancesDB();
            dataBlobs.Add(compInstances);
            OrderableDB ordable = new OrderableDB();
            dataBlobs.Add(ordable);
            var ship = Entity.Create(starsys, ownerFaction.Guid, dataBlobs);

            StaticDataStore staticdata = StaticRefLib.StaticData;
            ComponentDesigner fireControlDesigner;
            ComponentDesign integratedfireControl;


            ComponentTemplateSD bfcSD = staticdata.ComponentTemplates[new Guid("33fcd1f5-80ab-4bac-97be-dbcae19ab1a0")];
            fireControlDesigner = new ComponentDesigner(bfcSD, ownerFaction.GetDataBlob<FactionTechDB>());
            fireControlDesigner.Name = "Bridge Computer Systems";
            fireControlDesigner.ComponentDesignAttributes["Range"].SetValueFromInput(0);
            fireControlDesigner.ComponentDesignAttributes["Tracking Speed"].SetValueFromInput(0);
            fireControlDesigner.ComponentDesignAttributes["Size vs Range"].SetValueFromInput(0);

            //return fireControlDesigner.CreateDesign(faction);
            integratedfireControl = fireControlDesigner.CreateDesign(ownerFaction);
            ownerFaction.GetDataBlob<FactionTechDB>().IncrementLevel(integratedfireControl.TechID);

            //some DB's need tobe created after the entity.
            var namedb = new NameDB(ship.Guid.ToString());
            namedb.SetName(ownerFaction.Guid, shipName);

            ship.SetDataBlob(namedb);
            ship.SetDataBlob(orbit);
            
            EntityManipulation.AddComponentToEntity(ship, integratedfireControl, 1);

            foreach (var item in shipDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(ship, item.design, item.count);
            }

            if (ship.HasDataBlob<NewtonThrustAbilityDB>())
            {
                NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(ship);
            }
            
            
            
            return ship;
        }
    }
}
