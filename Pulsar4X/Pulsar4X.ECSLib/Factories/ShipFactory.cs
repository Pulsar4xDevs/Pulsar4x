using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    [JsonObject]
    public class ShipDesign : ICargoable, IConstrucableDesign, ISerializable
    {


        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.CanBeLaunched;
        public Guid ID { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid CargoTypeID { get; }
        public int DesignVersion = 0;
        public bool IsObsolete = false;
        public int Mass { get; }
        /// <summary>
        /// m^3
        /// </summary>
        public double Volume;
        public List<(ComponentDesign design, int count)> Components;
        public (ArmorSD type, float thickness) Armor;
        public Dictionary<Guid, int> ResourceCosts { get; internal set; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> MineralCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> MaterialCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> ComponentCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> ShipInstanceCost = new Dictionary<Guid, int>();
        public int CrewReq;
        public int IndustryPointCosts { get; }
        
        //TODO: this is one of those places where moddata has bled into hardcode...
        //the guid here is from IndustryTypeData.json "Ship Assembly"
        public Guid IndustryTypeID { get; } = new Guid("91823C5B-A71A-4364-A62C-489F0183EFB5");
        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, Guid productionLine, IndustryJob batchJob, IConstrucableDesign designInfo)
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
            faction.ShipDesigns.Add(ID, this);
            faction.IndustryDesigns[ID] = this;
            Name = name;
            Components = components;
            Armor = armor;

            
            foreach (var component in components)
            {
                Mass += component.design.Mass * component.count;
                CrewReq += component.design.CrewReq;
                CreditCost += component.design.CreditCost;
                Volume += component.design.Volume_m3 * component.count;
                if (ComponentCosts.ContainsKey(component.design.ID))
                {
                    ComponentCosts[component.design.ID] = ComponentCosts[component.design.ID] + component.count;
                }
                else
                {
                    ComponentCosts.Add(component.design.ID, component.count);
                }

            }
            
            
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = Mass;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
    public static class ShipFactory
    {
        
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Vector3 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            var distanceFromParent = parent.GetDataBlob<MassVolumeDB>().RadiusInM * 2;
            position.X += distanceFromParent;

            return CreateShip(shipDesign, ownerFaction, position, parent, starsys, shipName);
        }

        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, double angleRad, string shipName = null)
        {
            Vector3 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            
            var distanceFromParent = parent.GetDataBlob<MassVolumeDB>().RadiusInM * 2;

            var x = distanceFromParent * Math.Cos(angleRad);
            var y = distanceFromParent * Math.Sin(angleRad);
            
            var pos = new Vector3(position.X + x, position.Y + y, 0);
            
            StarSystem starsys = (StarSystem)parent.Manager;
            return CreateShip(shipDesign, ownerFaction, pos, parent, starsys, shipName);
        }
        
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Vector3 position, Entity parent, StarSystem starsys, string shipName = null)
        {

            
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            
            var shipinfo = new ShipInfoDB();
            dataBlobs.Add(shipinfo);
            var mvdb = MassVolumeDB.NewFromMassAndVolume(shipDesign.Mass, shipDesign.Volume);
            dataBlobs.Add(mvdb);
            PositionDB posdb = new PositionDB(Distance.MToAU(position), starsys.Guid, parent);
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
            OrbitDB orbit = OrbitDB.FromPosition(parent, ship, starsys.ManagerSubpulses.StarSysDateTime);
            ship.SetDataBlob(namedb);
            ship.SetDataBlob(orbit);

            EntityManipulation.AddComponentToEntity(ship, integratedfireControl, 1);

            foreach (var item in shipDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(ship, item.design, item.count);
            }

            if (ship.HasDataBlob<NewtonThrustAbilityDB>() && ship.HasDataBlob<CargoStorageDB>())
            {
                NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(ship);
            }

            return ship;
        }
    }
}
