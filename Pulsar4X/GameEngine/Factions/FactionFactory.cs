using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEngine.Engine.Orders;
using Newtonsoft.Json.Linq;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;
using Pulsar4X.Weapons;

namespace Pulsar4X.Factions
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
                    if(location.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB))
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
                    case "byVolume":
                        CargoTransferProcessor.AddRemoveCargoVolume(target, factionDataStore.CargoGoods[cargoId], amount);
                        break;
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
            factionInfo.FactionMaskIndex = game.AllocateFactionMaskIndex();

            var factionTechDB = new FactionTechDB();

            var blobs = new List<BaseDataBlob> {
                name,
                factionInfo,
                new FactionAbilitiesDB(),
                factionTechDB,
                new FactionOwnerDB(),
                new FleetDB(),
                new ActionQueueDB(),
            };
            var factionEntity = Entity.Create();
            game.GlobalManager.AddEntity(factionEntity, blobs);

            factionInfo.EventLog = FactionEventLog.Create(factionEntity.Id, game.TimePulse);
            factionInfo.EventLog.Subscribe();

            // Need to unlock the starting data in the game
            // foreach(var id in game.StartingGameData.DefaultItems["player-starting-items"].Items)
            // {
            //     factionInfo.Data.Unlock(id);

            //     // Research any tech that is listed
            //     if(factionInfo.Data.Techs.ContainsKey(id))
            //     {
            //         factionInfo.Data.IncrementTechLevel(id);
            //     }

            //     if(factionInfo.Data.CargoGoods.IsMaterial(id))
            //     {
            //         factionInfo.IndustryDesigns[id] = (IConstructableDesign)factionInfo.Data.CargoGoods[id];
            //     }
            // }

            // Add this faction to the SM's access list.
            game.SpaceMaster.SetAccess(factionEntity.Id, AccessRole.SM);
            name.SetName(factionEntity.Id, factionName);
            game.Factions.Add(factionEntity.Id, factionEntity);
            return factionEntity;
        }

        public static Entity CreateBasicFaction(Game game, string factionName, string abbreviation, int startingFunds)
        {
            var name = new NameDB(factionName);

            //var facinfo = new FactionInfoDB(new List<Entity>(), new List<Guid>(), );
            var factionInfo = new FactionInfoDB()
            {
                Abbreviation = abbreviation,
            };
            factionInfo.Data = new FactionDataStore(game.StartingGameData);
            factionInfo.FactionMaskIndex = game.AllocateFactionMaskIndex();
            factionInfo.Money.AddIncome(
                game.TimePulse.GameGlobalDateTime,
                TransactionCategory.InitialInvestment,
                "Add initial investments funds",
                startingFunds);

            var factionTechDB = new FactionTechDB();

            var blobs = new List<BaseDataBlob> {
                name,
                factionInfo,
                new FactionAbilitiesDB(),
                factionTechDB,
                new FactionOwnerDB(),
                new FleetDB(),
                new ActionQueueDB(),
            };
            var factionEntity = Entity.Create();
            game.GlobalManager.AddEntity(factionEntity, blobs);

            factionInfo.EventLog = FactionEventLog.Create(factionEntity.Id, game.TimePulse);
            factionInfo.EventLog.Subscribe();

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

        /// <summary>
        /// Places faction blueprints. They get fleets and a captain, and no colony.
        /// Call this after owned colonies so those factions already exist to receive a stance.
        /// No goals are assigned.
        /// </summary>
        public static void PlaceFactions(Game game, ModDataStore data)
        {
            foreach (var blueprint in data.Factions.Values)
                PlaceFaction(game, data, blueprint);
            SeedSurveyCharts(game);
        }

        static void PlaceFaction(Game game, ModDataStore data, FactionBlueprint blueprint)
        {
            if (string.IsNullOrEmpty(blueprint.Name) || string.IsNullOrEmpty(blueprint.Body))
                return;
            if (!TryFindBody(game, blueprint.System, blueprint.Body, out var system, out var body))
                return;

            var faction = CreateBasicFaction(
                game,
                blueprint.Name,
                string.IsNullOrEmpty(blueprint.Abbreviation) ? blueprint.Name : blueprint.Abbreviation,
                blueprint.StartingFunds);
            faction.FactionOwnerID = faction.Id;

            var info = faction.GetDataBlob<FactionInfoDB>();
            if (!info.KnownSystems.Contains(system.ID))
                info.KnownSystems.Add(system.ID);

            if (!string.IsNullOrEmpty(blueprint.Species)
                && data.Species.TryGetValue(blueprint.Species, out var speciesBlueprint))
            {
                var species = SpeciesFactory.CreateFromBlueprint(system, speciesBlueprint);
                species.FactionOwnerID = faction.Id;
                info.Species.Add(species);
            }

            // Designs look up resources, templates, and tech levels. A blueprint faction does not
            // run a colony StartingItems list, so open those libraries.
            UnlockAll(info.Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID), info);
            UnlockAll(info.Data.LockedComponentTemplates.Keys, info);
            UnlockAll(info.Data.LockedIndustryTypes.Keys, info);
            UnlockAll(info.Data.LockedCargoTypes.Keys, info);
            UnlockAll(info.Data.LockedArmor.Keys, info);
            var lockedTechs = info.Data.LockedTechs.Keys.ToList();
            UnlockAll(lockedTechs, info);
            foreach (var id in lockedTechs)
                info.Data.IncrementTechLevel(id);

            ComponentDesigner.StartResearched = true;
            foreach (var id in blueprint.ComponentDesigns ?? new List<string>())
                ComponentDesignFromJson.Create(faction, info.Data, game.StartingGameData.ComponentDesigns[id]);
            ComponentDesigner.StartResearched = false;

            foreach (var id in blueprint.ShipDesigns ?? new List<string>())
                ShipDesignFromJson.Create(faction, info.Data, game.StartingGameData.ShipDesigns[id]);

            foreach (var fleet in blueprint.Fleets ?? new List<FactionBlueprint.FleetBlueprint>())
            {
                var fleetEntity = FleetFactory.Create(system, faction.Id, fleet.Name);
                var fleetDB = fleetEntity.GetDataBlob<FleetDB>();
                fleetDB.SetParent(faction);
                if (fleet.Ships == null)
                    continue;

                foreach (var ship in fleet.Ships)
                {
                    double randomRadian = game.RNG.NextDouble() * Math.PI * 2;
                    var shipEntity = ShipFactory.CreateShip(
                        info.ShipDesigns[ship.DesignId], faction, body, randomRadian, ship.Name);
                    fleetDB.AddChild(shipEntity);

                    var commanderDB = CommanderFactory.CreateShipCaptain(game);
                    commanderDB.CommissionedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
                    commanderDB.RankedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365);
                    var commander = CommanderFactory.Create(system, faction.Id, commanderDB);
                    shipEntity.GetDataBlob<ShipInfoDB>().CommanderID = commander.Id;

                    if (fleetDB.FlagShipID < 0)
                        fleetDB.FlagShipID = shipEntity.Id;

                    LoadCargo(shipEntity, info.Data, ship.Cargo);
                }
            }

            if (Enum.TryParse<FactionStance>(blueprint.Stance, ignoreCase: true, out var stance))
                SetStanceTowardSystem(game, faction, system, stance);
        }

        /// <summary>
        /// Stores <paramref name="stance"/> both ways. Trade requires both sides.
        /// Factions qualify by knowing the system or by owning a colony or ship there.
        /// </summary>
        static void SetStanceTowardSystem(Game game, Entity faction, StarSystem system, FactionStance stance)
        {
            var info = faction.GetDataBlob<FactionInfoDB>();
            foreach (var other in game.Factions.Values)
            {
                if (other.Id == faction.Id || other.Id == game.GameMasterFaction.Id)
                    continue;
                if (!other.TryGetDataBlob<FactionInfoDB>(out var otherInfo))
                    continue;
                if (!SharesSystem(otherInfo, other.Id, system))
                    continue;

                info.Stances[other.Id] = stance;
                otherInfo.Stances[faction.Id] = stance;
            }
        }

        static bool SharesSystem(FactionInfoDB info, int factionId, StarSystem system)
        {
            if (info.KnownSystems.Contains(system.ID))
                return true;

            foreach (var colony in system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            {
                if (colony.FactionOwnerID == factionId)
                    return true;
            }

            foreach (var ship in system.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                if (ship.FactionOwnerID == factionId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Marks the nearest existing asteroids to Ceres surveyed for Strata and lists those
        /// charts on Ceres Depot. The depot does not gain the survey. No new bodies.
        /// </summary>
        static void SeedSurveyCharts(Game game)
        {
            Entity? survey = null;
            foreach (var faction in game.Factions.Values)
            {
                if (faction.GetDataBlob<NameDB>().DefaultName == CeresStart.SurveyFactionName)
                {
                    survey = faction;
                    break;
                }
            }
            if (survey == null || !survey.TryGetDataBlob<FactionInfoDB>(out var surveyInfo))
                return;

            Entity? depot = null;
            Entity? ceres = null;
            foreach (var system in game.Systems)
            {
                foreach (var colony in system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                {
                    if (colony.GetDataBlob<NameDB>().DefaultName != CeresStart.DepotFactionName)
                        continue;
                    if (!colony.HasDataBlob<LogiBaseDB>())
                        return;
                    depot = colony;
                    ceres = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                    break;
                }
                if (depot != null)
                    break;
            }
            if (depot == null || ceres?.Manager == null || !ceres.TryGetDataBlob<PositionDB>(out var ceresPos))
                return;

            var rocks = new List<(Entity body, double distance, int id)>();
            foreach (var body in ceres.Manager.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>())
            {
                if (body.GetDataBlob<SystemBodyInfoDB>().BodyType != BodyType.Asteroid)
                    continue;
                if (!body.HasDataBlob<GeoSurveyableDB>() || !body.TryGetDataBlob<MineralsDB>(out var minerals))
                    continue;
                if (minerals.Minerals.Count == 0 || !body.TryGetDataBlob<PositionDB>(out var pos))
                    continue;
                rocks.Add((body, pos.GetDistanceTo_m(ceresPos), body.Id));
            }

            rocks.Sort(static (a, b) =>
            {
                int byDistance = a.distance.CompareTo(b.distance);
                return byDistance != 0 ? byDistance : a.id.CompareTo(b.id);
            });

            int listed = 0;
            foreach (var (body, _, _) in rocks)
            {
                if (listed >= CeresStart.ChartCount)
                    break;
                var geo = body.GetDataBlob<GeoSurveyableDB>();
                geo.GeoSurveyStatus[survey.Id] = 0;
                body.GetDataBlob<MineralsDB>().GrantFactionPartialAccess(surveyInfo.FactionMask);
                if (!IntelBook.TryConsign(
                        depot,
                        survey,
                        IntelKind.Geo,
                        IntelBook.SubjectOf(body.Id),
                        CeresStart.ChartAsk,
                        out _))
                    continue;
                listed++;
            }
        }

        static void UnlockAll(IEnumerable<string> ids, FactionInfoDB info)
        {
            foreach (var id in ids.ToList())
                info.Data.Unlock(id);
        }

        static bool TryFindBody(Game game, string? systemId, string bodyName, out StarSystem system, out Entity body)
        {
            IEnumerable<StarSystem> systems = string.IsNullOrEmpty(systemId)
                ? game.Systems
                : game.Systems.Where(s => s.ID == systemId);

            foreach (var candidate in systems)
            {
                if (NameLookup.TryGetFirstEntityWithName(candidate, bodyName, out var found))
                {
                    system = candidate;
                    body = found;
                    return true;
                }
            }

            system = null!;
            body = null!;
            return false;
        }

        static void LoadCargo(Entity target, FactionDataStore factionDataStore, List<FactionBlueprint.CargoBlueprint>? cargo)
        {
            if (cargo == null)
                return;

            foreach (var item in cargo)
            {
                var type = item.Type ?? "byMass";
                switch (type)
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