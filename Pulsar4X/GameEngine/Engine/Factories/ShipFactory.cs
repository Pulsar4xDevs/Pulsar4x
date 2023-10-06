using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Engine
{
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
            var parentPosition = parent.GetDataBlob<PositionDB>().AbsolutePosition;
            var position = OrbitProcessor.GetPosition(orbit.GetElements(), parent.StarSysDateTime);
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
            var ship = Entity.Create();
            ship.FactionOwnerID = ownerFaction.Id;
            starsys.AddEntity(ship, dataBlobs);


            //some DB's need tobe created after the entity.
            var namedb = new NameDB(ship.Id.ToString());
            if (shipName == null)
            {
                shipName = NameFactory.GetShipName(ownerFaction.Manager.Game);
            }

            namedb.SetName(ownerFaction.Id, shipName);

            ship.SetDataBlob(namedb);
            ship.SetDataBlob(orbit);

            foreach (var item in shipDesign.Components)
            {
                ship.AddComponent(item.design, item.count);
            }

            if (ship.HasDataBlob<NewtonThrustAbilityDB>())
            {
                NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(ship);
            }

            return ship;
        }
    }
}