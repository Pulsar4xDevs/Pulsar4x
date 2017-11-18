using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class ShipFactory
    {
        public static Entity CreateShip(Entity classEntity, EntityManager systemEntityManager, Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Entity ship = CreateShip(classEntity, systemEntityManager, ownerFaction, parent.GetDataBlob<PositionDB>()?.AbsolutePosition ?? Vector4.Zero, starsys, shipName);

            var orbitDB = new OrbitDB(parent);
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
            protoShip.SetDataBlob(nameDB);

            var OwnedDB = new OwnedDB(ownerFaction);           
            protoShip.SetDataBlob(OwnedDB);

            OrderableDB orderableDB = new OrderableDB();
            protoShip.SetDataBlob(orderableDB);

            PositionDB position = new PositionDB(pos, starsys.Guid);
            protoShip.SetDataBlob(position);


            protoShip.SetDataBlob(new DesignInfoDB(classEntity));

            Entity shipEntity = new Entity(systemEntityManager, protoShip);
            FactionHelpers.SetOwnership(shipEntity, ownerFaction);

            //replace the ships references to the design's specific instances with shiny new specific instances
            ComponentInstancesDB componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>();
            var newSpecificInstances = new PrIwObsDict<Entity, PrIwObsList<Entity>>();
            foreach (var kvp in componentInstances.SpecificInstances)
            {
                newSpecificInstances.Add(kvp.Key, new PrIwObsList<Entity>());
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    newSpecificInstances[kvp.Key].Add(ComponentInstanceFactory.NewInstanceFromDesignEntity(kvp.Key, ownerFaction, systemEntityManager));
                }
            }
            componentInstances.SpecificInstances = newSpecificInstances;

            foreach (var componentType in shipEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances)
            {
                int numComponents = componentType.Value.Count;
                componentType.Value.Clear();

                for(int i=0; i < numComponents;i++)
                    EntityManipulation.AddComponentToEntity(shipEntity, componentType.Key);

                foreach (var componentInstance in componentType.Value)
                {
                    // Set the parent/owning Entity to the shipEntity
                    AttributeToAbilityMap.AddAbility(shipEntity, componentType.Key, componentInstance);

                    //TODO: do this somewhere else, recalcprocessor maybe?
                    if (componentInstance.HasDataBlob<SensorReceverAtbDB>())
                    {
                        var sensor = componentInstance.GetDataBlob<SensorReceverAtbDB>();
                        DateTime nextDatetime = shipEntity.Manager.ManagerSubpulses.SystemLocalDateTime + TimeSpan.FromSeconds(sensor.ScanTime);
                        shipEntity.Manager.ManagerSubpulses.AddEntityInterupt(nextDatetime, new SensorScan(), componentInstance);
                    }
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
                massVolumeDB
            };

            // now lets create the ship class:
            Entity shipClassEntity = new Entity(game.GlobalManager, shipDBList); 

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
