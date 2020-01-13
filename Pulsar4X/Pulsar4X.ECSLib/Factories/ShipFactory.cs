using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    
    public class ShipDesign : ICargoable, IConstrucableDesign
    {
        public Guid ID { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid CargoTypeID { get; }
        public int DesignVersion = 0;
        public bool IsObsolete = false;
        public int Mass { get; }
        public double Volume;
        public List<(ComponentDesign design, int count)> Components;
        public (string name, double density, float thickness) Armor;
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
        public int CreditCost;
        public EntityDamageProfileDB DamageProfileDB;




        public ShipDesign(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (string name, double density, float thickness) armor)
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
                ComponentCosts.Add(component.design.ID, component.count);
            }
            
            
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = Mass;
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
            
            //some DB's need tobe created after the entity.
            var namedb = new NameDB(ship.Guid.ToString());
            namedb.SetName(ownerFaction.Guid, shipName);
            OrbitDB orbit = OrbitDB.FromPosition(parent, ship, starsys.ManagerSubpulses.StarSysDateTime);
            ship.SetDataBlob(namedb);
            ship.SetDataBlob(orbit);

            foreach (var item in shipDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(ship, item.design, item.count);
            }

            if (ship.HasDataBlob<NewtonThrustAbilityDB>() && ship.HasDataBlob<CargoStorageDB>())
            {
                NewtonionMovementProcessor.CalcDeltaV(ship);
            }

            return ship;
        }



    }
}
