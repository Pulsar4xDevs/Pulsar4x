using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Designs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

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
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, string? shipName = null)
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
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Entity parent, double angleRad, string? shipName = null)
        {


            var distanceFromParent = parent.GetDataBlob<MassVolumeDB>().RadiusInM * 2;

            var x = distanceFromParent * Math.Cos(angleRad);
            var y = distanceFromParent * Math.Sin(angleRad);

            var pos = new Vector3( x,  y, 0);
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
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, Vector3 position, Entity parent, string? shipName = null)
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
        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction,  KeplerElements ke, Entity parent, string? shipName = null)
        {
            OrbitDB orbit = OrbitDB.FromKeplerElements(parent,shipDesign.MassPerUnit, ke, parent.StarSysDateTime);
            var position =  OrbitMath.GetPosition(ke, parent.StarSysDateTime);
            return CreateShip(shipDesign, ownerFaction, orbit, parent, shipName);
        }

        public static Entity CreateShip(ShipDesign shipDesign, Entity ownerFaction, OrbitDB orbit,  Entity parent, string? shipName = null)
        {

            var starsys = parent.Manager;
            var position = OrbitMath.GetPosition(orbit, parent.StarSysDateTime);
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();

            var shipinfo = new ShipInfoDB(shipDesign);
            dataBlobs.Add(shipinfo);
            var mvdb = MassVolumeDB.NewFromMassAndVolume(shipDesign.MassPerUnit, shipDesign.VolumePerUnit);
            dataBlobs.Add(mvdb);
            PositionDB posdb = new PositionDB(position, starsys.ManagerID, parent);
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
            if (string.IsNullOrEmpty(shipName))
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

        public static void DestroyShip(Entity shipToDestroy)
        {
            // Steps:
            // - Remove the ship from fleet (if any)
            // - Remove the ship as the fleet flagship (if set)
            // - Kill any officers on board
            // - Create wreckage
            // - Remove the ship entity from the game
            
            var game = shipToDestroy.Manager.Game;
            var faction = game.Factions[shipToDestroy.FactionOwnerID];
            
            // Remove the ship from its fleet
            if(faction.TryGetDatablob<FleetDB>(out var fleetDB))
            {
                // Recursively try to get the fleet the ship belongs to
                var belongsToFleet = fleetDB.TryGetChild<FleetDB>(shipToDestroy);

                // If we found it send out the order to unassign the ship
                if(belongsToFleet != null && belongsToFleet.OwningEntity != null)
                {
                    // The unassign ship command removes the ship from the fleet
                    // and checks if it is the flagship and removes that also
                    var command = FleetOrder.UnassignShip(
                        shipToDestroy.FactionOwnerID,
                        belongsToFleet.OwningEntity,
                        shipToDestroy);
                    
                    game.OrderHandler.HandleOrder(command);
                }
            }

            // Kill any officers on board
            // (currently just the commander)
            // TODO: check for additional people on board (passengers, officers, scientists etc)
            if(shipToDestroy.TryGetDatablob<ShipInfoDB>(out var shipInfoDB) 
                && shipToDestroy.Manager.TryGetEntityById(shipInfoDB.CommanderID, out var commanderEntity))
            {
                CommanderFactory.DestroyCommander(commanderEntity);
            }


            // Remove the ship entity from the game
            shipToDestroy.Destroy();
        }
    }
}