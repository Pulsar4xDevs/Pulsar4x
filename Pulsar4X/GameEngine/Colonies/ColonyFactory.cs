using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Components;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;
using System;
using Pulsar4X.Technology;

namespace Pulsar4X.Colonies
{
    public static class ColonyFactory
    {
        public static Entity CreateFromBlueprint(Game game, Entity faction, Entity species, StarSystem startingSystem, Entity systemBody, ColonyBlueprint colonyBlueprint)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();

            // Unlock the starting items
            foreach(var id in colonyBlueprint.StartingItems)
            {
                factionInfo.Data.Unlock(id);

                // Research any tech that is listed
                if(factionInfo.Data.Techs.ContainsKey(id))
                {
                    factionInfo.Data.IncrementTechLevel(id);
                }

                if(factionInfo.Data.CargoGoods.IsMaterial(id))
                {
                    factionInfo.IndustryDesigns[id] = (IConstructableDesign)factionInfo.Data.CargoGoods[id];
                }
            }

            // Add component designs
            ComponentDesigner.StartResearched = true;
            foreach(var id in colonyBlueprint.ComponentDesigns)
            {
                ComponentDesignFromJson.Create(faction, factionInfo.Data, game.StartingGameData.ComponentDesigns[id]);
            }
            ComponentDesigner.StartResearched = false;

            // Add ship designs
            foreach(var id in colonyBlueprint.ShipDesigns)
            {
                ShipDesignFromJson.Create(faction, factionInfo.Data, game.StartingGameData.ShipDesigns[id]);
            }

            var blobs = new List<BaseDataBlob>();

            string planetName = systemBody.GetDataBlob<NameDB>().GetName(faction.Id);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            name.SetName(faction.Id, name.DefaultName);

            var pos = new Vector3(systemBody.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0);

            blobs.Add(name);
            blobs.Add(new ColonyInfoDB(species, (long)(colonyBlueprint.StartingPopulation ?? 1000), systemBody));
            blobs.Add(new ColonyBonusesDB());
            blobs.Add(new MiningDB());
            blobs.Add(new OrderableDB());
            blobs.Add(new MassVolumeDB());
            blobs.Add(new CargoStorageDB());
            blobs.Add(new PositionDB(pos, systemBody));
            blobs.Add(new TeamsHousedDB());
            blobs.Add(new ComponentInstancesDB()); //installations get added to the componentInstancesDB

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = faction.Id;
            systemBody.Manager.AddEntity(colonyEntity, blobs);
            factionInfo.Colonies.Add(colonyEntity);
            faction.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);

            // Add starting installations
            foreach(var installation in colonyBlueprint.Installations)
            {
                colonyEntity.AddComponent(
                    factionInfo.InternalComponentDesigns[installation.Id],
                    (int)installation.Amount
                );
            }

            // Add starting colony cargo
            LoadCargo(colonyEntity, factionInfo.Data, colonyBlueprint.Cargo);

            // Add a starting scientist
            // TODO: load people from blueprints
            var scientistEntity = CommanderFactory.CreateScientist(faction, colonyEntity);
            colonyEntity.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

            // Add starting fleets
            foreach(var fleet in colonyBlueprint.Fleets)
            {
                var fleetEntity = FleetFactory.Create(startingSystem, faction.Id, fleet.Name);
                var fleetDB = fleetEntity.GetDataBlob<FleetDB>();
                fleetDB.SetParent(faction);
                if(fleet.Ships == null) continue;

                foreach(var ship in fleet.Ships)
                {
                    var shipEntity = ShipFactory.CreateShip(factionInfo.ShipDesigns[ship.DesignId], faction, systemBody, ship.Name);
                    fleetDB.AddChild(shipEntity);

                    var commanderDB = CommanderFactory.CreateShipCaptain(game);
                    commanderDB.CommissionedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
                    commanderDB.RankedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365);
                    var commander = CommanderFactory.Create(startingSystem, faction.Id, commanderDB);
                    shipEntity.GetDataBlob<ShipInfoDB>().CommanderID = commander.Id;

                    if(fleetDB.FlagShipID < 0)
                        fleetDB.FlagShipID = shipEntity.Id;

                    LoadCargo(shipEntity, factionInfo.Data, ship.Cargo);
                }
            }

            return colonyEntity;
        }


        /// <summary>
        /// Creates a new colony with zero population unless specified.
        /// </summary>
        public static Entity CreateColony(Entity factionEntity, Entity speciesEntity, Entity planetEntity, long initialPopulation = 0)
        {
            var blobs = new List<BaseDataBlob>();

            string planetName = planetEntity.GetDataBlob<NameDB>().GetName(factionEntity.Id);
            NameDB name = new NameDB(planetName + " Colony"); // TODO: Review default name.
            name.SetName(factionEntity.Id, name.DefaultName);

            var pos = new Vector3(planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0);

            blobs.Add(name);
            blobs.Add(new ColonyInfoDB(speciesEntity, initialPopulation, planetEntity));
            blobs.Add(new ColonyBonusesDB());
            blobs.Add(new MiningDB());
            blobs.Add(new OrderableDB());
            blobs.Add(new MassVolumeDB());
            blobs.Add(new CargoStorageDB());
            blobs.Add(new PositionDB(pos, planetEntity));
            blobs.Add(new TeamsHousedDB());
            blobs.Add(new ComponentInstancesDB()); //installations get added to the componentInstancesDB

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = factionEntity.Id;
            planetEntity.Manager.AddEntity(colonyEntity, blobs);
            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            factionInfo.Colonies.Add(colonyEntity);
            factionEntity.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);
            return colonyEntity;
        }

        private static void LoadCargo(Entity target, FactionDataStore factionDataStore, List<ColonyBlueprint.StartingItemBlueprint>? cargo)
        {
            if(cargo == null) return;

            foreach(var item in cargo)
            {
                var type = item.Type ?? "byMass";

                switch(type)
                {
                    case "byVolume":
                        CargoTransferProcessor.AddRemoveCargoVolume(target, factionDataStore.CargoGoods[item.Id], item.Amount);
                        break;
                    case "byCount":
                        CargoTransferProcessor.AddCargoItems(target, factionDataStore.CargoGoods[item.Id], (int)item.Amount);
                        break;
                    default:
                        CargoTransferProcessor.AddRemoveCargoMass(target, factionDataStore.CargoGoods[item.Id], item.Amount);
                        break;
                }
            }
        }
    }
}