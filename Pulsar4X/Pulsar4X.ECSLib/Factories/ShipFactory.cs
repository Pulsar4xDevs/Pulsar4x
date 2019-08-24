using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.ECSLib
{
    public static class ShipFactory
    {
        
        public class ShipClass
        {
            public Guid Guid = Guid.NewGuid();
            public string DesignName;
            public int DesignVersion = 0;
            public bool IsObsolete = false;
            public double Mass;
            public double Volume;
            public List<(ComponentDesign design, int count)> Components;
            public (string name, double density, float thickness) Armor;
            public Dictionary<MineralSD, int> MineralCost;
            public Dictionary<ProcessedMaterialSD, int> MaterialCost;
            public Dictionary<ComponentDesign, int> ComponentCost;
            public int CrewReq;
            public int BuildPointCost;
            public int CreditCost;
            public EntityDamageProfileDB DamageProfileDB;


            public ShipClass(FactionInfoDB factionInfoDB )
            {
                factionInfoDB.ShipDesigns.Add(this);
            }

            public ShipClass(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (string name, double density, float thickness) armor)
            {
                faction.ShipDesigns.Add(this);
                DesignName = name;
                Components = components;
                Armor = armor;
            }


        }

        public static Entity CreateShip(ShipClass shipClass, Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Vector3 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            var distanceFromParent = Distance.AuToMt(parent.GetDataBlob<MassVolumeDB>().Radius * 2);
            position.X += distanceFromParent;

            return CreateShip(shipClass, ownerFaction, position, parent, starsys, shipName);
        }

        public static Entity CreateShip(ShipClass shipClass, Entity ownerFaction, Vector3 position, Entity parent, StarSystem starsys, string shipName = null)
        {

            
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            
            var shipinfo = new ShipInfoDB();
            dataBlobs.Add(shipinfo);
            var mvdb = MassVolumeDB.NewFromMassAndVolume(shipClass.Mass, shipClass.Volume);
            dataBlobs.Add(mvdb);
            PositionDB posdb = new PositionDB(Distance.MToAU(position), starsys.Guid, parent);
            dataBlobs.Add(posdb);
            EntityDamageProfileDB damagedb = (EntityDamageProfileDB)shipClass.DamageProfileDB.Clone(); 
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

            foreach (var item in shipClass.Components)
            {
                EntityManipulation.AddComponentToEntity(ship, item.design, item.count);
            }
            
            return ship;
        }



    }
}
