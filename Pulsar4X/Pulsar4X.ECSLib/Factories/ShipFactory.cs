using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class ShipFactory
    {
        public static Entity CreateShip(Entity classEntity, EntityManager systemEntityManager, Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Vector4 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            position.Y += Distance.KmToAU(10000);
            Entity ship = CreateShip(classEntity, systemEntityManager, ownerFaction,  position, starsys, shipName);

            var orbitDB = ShipMovementProcessor.CreateOrbitHereWithSemiMajAxis(ship, parent, 10000, systemEntityManager.ManagerSubpulses.SystemLocalDateTime);
            ship.GetDataBlob<PositionDB>().SetParent(orbitDB.Parent);
            ship.SetDataBlob(orbitDB);

            return ship;
        }
        public static Entity CreateShip(Entity classEntity, EntityManager systemEntityManager, Entity ownerFaction, Vector4 pos, StarSystem starsys, string shipName = null)
        {
            // @todo replace ownerFaction with formationDB later. Now ownerFaction used just to add name 
            // @todo: make sure each component design and component instance is unique, not duplicated
            ProtoEntity protoShip = classEntity.Clone();

            ShipInfoDB shipInfoDB = protoShip.GetDataBlob<ShipInfoDB>();
            shipInfoDB.ShipClassDefinition = classEntity.Guid;

            if (shipName == null)
            {
                shipName = "Ship Name";
            } 

            NameDB nameDB = new NameDB(shipName);
            nameDB.SetName(ownerFaction.Guid, shipName);
            protoShip.SetDataBlob(nameDB);

            OrderableDB orderableDB = new OrderableDB();
            protoShip.SetDataBlob(orderableDB);

            PositionDB position = new PositionDB(pos, starsys.Guid);
            protoShip.SetDataBlob(position);


            protoShip.SetDataBlob(new DesignInfoDB(classEntity));

            //replace the ships references to the design's specific instances with shiny new specific instances

            ComponentInstancesDB classInstances = classEntity.GetDataBlob<ComponentInstancesDB>();


            Entity shipEntity = new Entity(systemEntityManager, ownerFaction.Guid, protoShip);
            shipEntity.RemoveDataBlob<ComponentInstancesDB>();
            shipEntity.SetDataBlob(new ComponentInstancesDB());
            if(shipEntity.HasDataBlob<FireControlAbilityDB>())
                shipEntity.RemoveDataBlob<FireControlAbilityDB>();

            foreach (var designKVP in classInstances.DesignsAndComponentCount)
            {
                for (int i = 0; i < designKVP.Value; i++)
                {
                    Entity newInstance = ComponentInstanceFactory.NewInstanceFromDesignEntity(designKVP.Key, ownerFaction.Guid, systemEntityManager);
                    EntityManipulation.AddComponentToEntity(shipEntity, newInstance);
                }
            }



            FactionOwnerDB factionOwner = ownerFaction.GetDataBlob<FactionOwnerDB>();
            factionOwner.SetOwned(shipEntity);
            ComponentInstancesDB shipComponentInstanceDB = shipEntity.GetDataBlob<ComponentInstancesDB>();

            //TODO: do this somewhere else, recalcprocessor maybe?
            foreach (var design in shipComponentInstanceDB.GetDesignsByType(typeof(SensorReceverAtbDB)))
            {
                foreach (var instance in shipComponentInstanceDB.GetComponentsBySpecificDesign(design.Guid))
                {
                    var sensor = design.GetDataBlob<SensorReceverAtbDB>();
                    DateTime nextDatetime = shipEntity.Manager.ManagerSubpulses.SystemLocalDateTime + TimeSpan.FromSeconds(sensor.ScanTime);
                    shipEntity.Manager.ManagerSubpulses.AddEntityInterupt(nextDatetime, new SensorScan().TypeName, instance.OwningEntity);

                }
            }   
            

            ReCalcProcessor.ReCalcAbilities(shipEntity);
            return shipEntity;
        }

        public static Entity CreateNewShipClass(Game game, Entity faction, string className = null)
        {
            //check className before any to use it in NameDB constructor
            if (string.IsNullOrEmpty(className))
            {
                ///< @todo source the class name from faction theme.
                className = "New Class"; // <- Hack for now.
            }

            // lets start by creating all the Datablobs that make up a ship class: TODO only need to add datablobs for compoents it has abilites for.
            var shipInfo = new ShipInfoDB();
            var armor = new ArmorDB();
            var buildCost = new BuildCostDB();
            var cargotype = new CargoAbleTypeDB();
            var crew = new CrewDB();
            var damage = new DamageDB();
            var maintenance = new MaintenanceDB();
            var sensorProfile = new SensorProfileDB();
            var name = new NameDB(className);
            name.SetName(faction.Guid, className);
            var componentInstancesDB = new ComponentInstancesDB();
            var massVolumeDB = new MassVolumeDB();

            // now lets create a list of all these datablobs so we can create our new entity:
            List<BaseDataBlob> shipDBList = new List<BaseDataBlob>()
            {
                shipInfo,
                armor,
                buildCost,
                cargotype,
                crew,
                damage,
                maintenance,
                sensorProfile,
                name,
                componentInstancesDB,
                massVolumeDB,
            };

            // now lets create the ship class:
            Entity shipClassEntity = new Entity(game.GlobalManager, faction.Guid, shipDBList); 
            FactionOwnerDB factionOwner = faction.GetDataBlob<FactionOwnerDB>();
            factionOwner.SetOwned(shipClassEntity);
            // also gets factionDB:
            FactionInfoDB factionDB = faction.GetDataBlob<FactionInfoDB>();

            // and add it to the faction:
            factionDB.ShipClasses.Add(shipClassEntity);

            // now lets set some ship info:
            shipInfo.ShipClassDefinition = Guid.Empty; // just make sure it is marked as a class and not a ship.

            // now lets add some components:
            ///< @todo Add ship components
            // -- basic armour of current faction tech level
            // -- minimum crew quaters defaulting to 3 months deployment time.
            // -- a bridge
            // -- an engineering space
            // -- a fuel tank
            
            // now update the ship system DBs to reflect the components:
            ///< @todo update ship to reflect added components

            return shipClassEntity;
        }


    }
}
