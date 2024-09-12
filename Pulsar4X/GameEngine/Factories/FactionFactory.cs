using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine
{

    public static class FactionFactory
    {
        /*
         *Stuff a faction needs to know:
         *name (nameDB)
         *password (AuthDB)
         *researched tech. (techDB)
         *
         *Owned Entites
         *
         *Sensor Contacts - these will be owned entites anyway.
         *  -System Bodies
         *  -Non Owned Entites
         *      -Colones
         *      -Ships
         *
         *Sensor Types
         *  - Grav, ie detecting anomalies in paths of known objects. - slow, but will find large dark planets.
         *  - Passive EM Spectrum:
         *      - Emited visable light (suns)
         *      - Reflected visable light (planets, moons)
         *      - Emitted IR (colonies, ship drives)
         *      - Reflected IR
         *      - Comms emmisions (colonies, ships)
         *  - Active EM:
         *      - Emmitting EM and looking for an echo. (radar)
         *
         * Owned Enties and Sensor Contacts need to be broken down by system.
         *
         *
         */

        public static Entity LoadFromJson(Game game, string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            var rootDirectory = (string?)Path.GetDirectoryName(filePath) ?? "Data/basemod/";
            var rootJson = JObject.Parse(fileContents);

            var name = rootJson["name"].ToString();
            var faction = CreateFaction(game, name);
            var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
            var factionDataStore = factionInfoDB.Data;

            var componentDesignsToLoad = (JArray?)rootJson["componentDesigns"];
            foreach(var componentDesignToLoad in componentDesignsToLoad)
            {
                string path = componentDesignToLoad.ToString();
                string fullPath = Path.Combine(rootDirectory, path);

                if(Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        ComponentDesignFromJson.Create(faction, factionDataStore, file);
                    }
                }
                else
                {
                    ComponentDesignFromJson.Create(faction, factionDataStore, fullPath);
                }
            }

            var ordnanceDesignsToLoad = (JArray?)rootJson["ordnanceDesigns"];
            foreach(var ordnanceDesignToLoad in ordnanceDesignsToLoad)
            {
                string path = ordnanceDesignToLoad.ToString();
                string fullPath = Path.Combine(rootDirectory, path);

                if(Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        OrdnanceDesignFromJson.Create(faction, file);
                    }
                }
                else
                {
                    OrdnanceDesignFromJson.Create(faction, fullPath);
                }
            }

            var shipDesignsToLoad = (JArray?)rootJson["shipDesigns"];
            foreach(var shipDesignToLoad in shipDesignsToLoad)
            {
                string path = shipDesignToLoad.ToString();
                string fullPath = Path.Combine(rootDirectory, path);

                if(Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        ShipDesignFromJson.Create(faction, factionDataStore, file);
                    }
                }
                else
                {
                    ShipDesignFromJson.Create(faction, factionDataStore, fullPath);
                }
            }

            var speciesToLoad = (JArray?)rootJson["species"];
            foreach(var toLoad in speciesToLoad)
            {
                string path = toLoad.ToString();
                string fullPath = Path.Combine(rootDirectory, path);

                if(Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        SpeciesFactory.CreateFromJson(faction, game.GlobalManager, file);
                    }
                }
                else
                {
                    SpeciesFactory.CreateFromJson(faction, game.GlobalManager, fullPath);
                }
            }

            var coloniesToLoad = (JArray?)rootJson["colonies"];
            if(coloniesToLoad != null)
            {
                foreach(var colonyToLoad in coloniesToLoad)
                {
                    var systemId = colonyToLoad["systemId"].ToString();

                    var system = game.Systems.Find(s => s.ID.Equals(systemId));
                    if(system == null) throw new NullReferenceException("invalid systemId in json");
                    var location = NameLookup.GetFirstEntityWithName(system, colonyToLoad["location"].ToString());

                    // Mark the colony location as geo surveyed
                    if(location.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB))
                    {
                        geoSurveyableDB.GeoSurveyStatus[faction.Id] = 0;
                    }

                    var speciesName = colonyToLoad["species"]["name"].ToString();
                    var species = faction.GetDataBlob<FactionInfoDB>().Species.Find(s => s.GetOwnersName().Equals(speciesName));
                    if(species == null) throw new NullReferenceException("invalid species name in json");
                    var population = (long?)colonyToLoad["species"]["population"] ?? 0;

                    var colony = ColonyFactory.CreateColony(faction, species, location, population);

                    var installationsToAdd = (JArray?)colonyToLoad["installations"];
                    if(installationsToAdd != null)
                    {
                        foreach(var install in installationsToAdd)
                        {
                            var installId = install["id"].ToString();
                            var amount = (int?)install["amount"] ?? 1;

                            colony.AddComponent(
                                factionInfoDB.InternalComponentDesigns[installId],
                                amount
                            );
                        }
                    }

                    LoadCargo(colony, factionDataStore, (JArray?)colonyToLoad["cargo"]);

                    //TODO: optionally set this from json
                    Scientist scientistEntity = CommanderFactory.CreateScientist(faction, colony);
                    colony.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

                    ReCalcProcessor.ReCalcAbilities(colony);
                }
            }

            var fleetsToLoad = (JArray?)rootJson["fleets"];
            if(fleetsToLoad != null)
            {
                foreach(var fleetToLoad in fleetsToLoad)
                {
                    var fleetName = (string?)fleetToLoad["name"] ?? NameFactory.GetFleetName(game);
                    var systemId = fleetToLoad["location"]["systemId"].ToString();
                    var system = game.Systems.Find(s => s.ID.Equals(systemId));
                    if(system == null) throw new NullReferenceException("invalid systemId in json");
                    var location = NameLookup.GetFirstEntityWithName(system, fleetToLoad["location"]["body"].ToString());

                    var fleet = FleetFactory.Create(system, faction.Id, fleetName);
                    var fleetDB = fleet.GetDataBlob<FleetDB>();
                    fleetDB.SetParent(faction);

                    var shipsInFleet = (JArray?)fleetToLoad["ships"];
                    if(shipsInFleet != null)
                    {
                        foreach(var shipToLoad in shipsInFleet)
                        {
                            var designId = shipToLoad["designId"].ToString();
                            var shipName = (string?)shipToLoad["name"] ?? NameFactory.GetShipName(game);
                            var ship = ShipFactory.CreateShip(factionInfoDB.ShipDesigns[designId], faction, location, shipName);
                            fleetDB.AddChild(ship);

                            var commanderDB = CommanderFactory.CreateShipCaptain(game);
                            commanderDB.CommissionedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
                            commanderDB.RankedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365);
                            var commander = CommanderFactory.Create(system, faction.Id, commanderDB);
                            ship.GetDataBlob<ShipInfoDB>().CommanderID = commander.Id;

                            if(fleetDB.FlagShipID < 0)
                                fleetDB.FlagShipID = ship.Id;

                            LoadCargo(ship, factionDataStore, (JArray?)shipToLoad["cargo"]);
                        }
                    }
                }
            }

            return faction;
        }

        private static void LoadCargo(Entity target, FactionDataStore factionDataStore, JArray? cargoArray)
        {
            if(cargoArray == null) return;

            foreach(var toAdd in cargoArray)
            {
                var cargoId = toAdd["id"].ToString();
                var amount = (int?)toAdd["amount"] ?? 1;
                var type = (string?)toAdd["type"] ?? "byMass";

                switch(type)
                {
                    case "byCount":
                        CargoTransferProcessor.AddCargoItems(target, factionDataStore.CargoGoods[cargoId], amount);
                        break;
                    default:
                        CargoTransferProcessor.AddRemoveCargoMass(target, factionDataStore.CargoGoods[cargoId], amount);
                        break;
                }
            }
        }


        public static Entity CreateFaction(Game game, string factionName)
        {
            var name = new NameDB(factionName);

            //var facinfo = new FactionInfoDB(new List<Entity>(), new List<Guid>(), );
            var factionInfo = new FactionInfoDB();
            factionInfo.Data = new FactionDataStore(game.StartingGameData);

            var factionTechDB = new FactionTechDB();

            var blobs = new List<BaseDataBlob> {
                name,
                factionInfo,
                new FactionAbilitiesDB(),
                factionTechDB,
                new FactionOwnerDB(),
                new FleetDB(),
                new OrderableDB(),
            };
            var factionEntity = Entity.Create();
            game.GlobalManager.AddEntity(factionEntity, blobs);

            factionInfo.EventLog = FactionEventLog.Create(factionEntity.Id, game.TimePulse);
            factionInfo.EventLog.Subscribe();

            // Need to unlock the starting data in the game
            foreach(var id in game.StartingGameData.DefaultItems["player-starting-items"].Items)
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

            // Add this faction to the SM's access list.
            game.SpaceMaster.SetAccess(factionEntity.Id, AccessRole.SM);
            name.SetName(factionEntity.Id, factionName);
            game.Factions.Add(factionEntity.Id, factionEntity);
            return factionEntity;
        }


        public static Entity CreatePlayerFaction(Game game, Player owningPlayer, string factionName)
        {
            Entity faction = CreateFaction(game, factionName);


            if (!Equals(owningPlayer, game.SpaceMaster))
            {
                owningPlayer.SetAccess(faction.Id, AccessRole.Owner);
            }

            return faction;
        }

        public static Entity CreateSpaceMasterFaction(Game game, Player owningPlayer, string factionName)
        {
            Entity faction = CreatePlayerFaction(game, owningPlayer, factionName);

            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            factionInfo.EventLog.Unsubscribe();
            factionInfo.EventLog = SpaceMasterEventLog.Create();
            factionInfo.EventLog.Subscribe();

            return faction;
        }


    }
}