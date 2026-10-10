using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Modding;
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
using Pulsar4X.Logistics;
using Pulsar4X.Ships;
using System;
using GameEngine.Engine.Orders;
using GameEngine.People;

namespace Pulsar4X.Colonies
{
    public static class ColonyFactory
    {
        public const string DEFAULT_SUFFIX = "HQ";

        public static Entity CreateFromBlueprint(Game game, Entity faction, Entity species, StarSystem startingSystem, Entity systemBody, ColonyBlueprint colonyBlueprint)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();

            // Unlock the starting items
            foreach(var id in colonyBlueprint.StartingItems ?? new List<string>())
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
            foreach(var id in colonyBlueprint.ComponentDesigns ?? new List<string>())
            {
                ComponentDesignFromJson.Create(faction, factionInfo.Data, game.StartingGameData.ComponentDesigns[id]);
            }
            ComponentDesigner.StartResearched = false;

            // Add ship designs
            foreach(var id in colonyBlueprint.ShipDesigns ?? new List<string>())
            {
                ShipDesignFromJson.Create(faction, factionInfo.Data, game.StartingGameData.ShipDesigns[id]);
            }

            var blobs = new List<BaseDataBlob>();

            string planetName = systemBody.GetDataBlob<NameDB>().GetName(faction.Id);
            // Player starts keep the "Planet HQ" name. A placed faction colony uses the blueprint name.
            string colonyName = !string.IsNullOrEmpty(colonyBlueprint.OwnerFaction) && !string.IsNullOrEmpty(colonyBlueprint.Name)
                ? colonyBlueprint.Name
                : $"{planetName} {DEFAULT_SUFFIX}";
            NameDB name = new NameDB(colonyName);
            name.SetName(faction.Id, name.DefaultName);

            var pos = new Vector3(systemBody.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0);

            blobs.Add(name);
            blobs.Add(new ColonyInfoDB(species, (long)(colonyBlueprint.StartingPopulation ?? 1000), systemBody));
            blobs.Add(new ColonyBonusesDB());
            blobs.Add(new MiningDB());
            blobs.Add(new ActionQueueDB());
            blobs.Add(new MassVolumeDB());
            blobs.Add(new CargoStorageDB());
            blobs.Add(new PositionDB(pos, systemBody));
            blobs.Add(new TeamsHousedDB());
            blobs.Add(new ComponentInstancesDB()); //installations get added to the componentInstancesDB
            blobs.Add(new InfrastructureDB()); //capacity gets summed from installations as they're added

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = faction.Id;
            systemBody.Manager.AddEntity(colonyEntity, blobs);
            factionInfo.Colonies.Add(colonyEntity);
            faction.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);

            // Grant faction access to mineral data on this planet
            if (systemBody.TryGetDataBlob<MineralsDB>(out var mineralsDB))
            {
                mineralsDB.GrantFactionAccess(factionInfo.FactionMask);
            }

            // Add starting installations
            foreach(var installation in colonyBlueprint.Installations ?? new List<ColonyBlueprint.StartingItemBlueprint>())
            {
                colonyEntity.AddComponent(
                    factionInfo.InternalComponentDesigns[installation.Id],
                    (int)installation.Amount
                );
            }

            // Add starting colony cargo
            LoadCargo(colonyEntity, factionInfo.Data, colonyBlueprint.Cargo);

            // Add starting launch queue entries
            if (colonyBlueprint.LaunchQueue != null && colonyEntity.TryGetDataBlob<LaunchComplexDB>(out var launchDB))
            {
                foreach (var entry in colonyBlueprint.LaunchQueue)
                {
                    string shipName = entry.Name ?? NameFactory.GetShipName(game);
                    launchDB.LaunchQueue.Add(new LaunchQueueEntry
                    {
                        DesignId = entry.DesignId,
                        ShipName = shipName
                    });
                }
            }

            // Add a starting scientist
            // TODO: load people from blueprints
            var scientistEntity = CommanderFactory.CreateScientist(faction, colonyEntity);
            colonyEntity.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

            if (colonyBlueprint.SeatAdministrator)
                SeatStartingAdministrator(game, faction, startingSystem, colonyEntity);

            // Add starting fleets
            foreach(var fleet in colonyBlueprint.Fleets ?? new List<ColonyBlueprint.FleetBlueprint>())
            {
                var fleetEntity = FleetFactory.Create(startingSystem, faction.Id, fleet.Name);
                var fleetDB = fleetEntity.GetDataBlob<FleetDB>();
                fleetDB.SetParent(faction);
                if(fleet.Ships == null) continue;

                foreach(var ship in fleet.Ships)
                {
                    double randomRadian = game.RNG.NextDouble() * Math.PI * 2;
                    var shipEntity = ShipFactory.CreateShip(factionInfo.ShipDesigns[ship.DesignId], faction, systemBody, randomRadian, ship.Name);
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

            // Cargo is already in the warehouse. A logistics office keeps listing that stock.
            // Priced rows go in first. Offer stock skips a cargo id that already has a policy row.
            if (colonyBlueprint.UniqueID == CeresStart.DepotColonyId)
                WriteDepotPrices(colonyEntity);
            if (colonyEntity.HasDataBlob<LogiBaseDB>())
                AgentProcessor.AssignGoal(colonyEntity, new Goal(GoalType.OfferStock) { Name = "Offer stock" });

            return colonyEntity;
        }

        /// <summary>
        /// Places colony blueprints that name a body and no owner. They belong to the player
        /// faction that already started, using that faction's species and unlocked designs.
        /// Call this after the start colony so its StartingItems have opened the libraries.
        /// </summary>
        public static void PlacePlayerColonies(Game game, ModDataStore data, Entity playerFaction, Entity playerSpecies, string? exceptColonyId = null)
        {
            var info = playerFaction.GetDataBlob<FactionInfoDB>();
            foreach (var colony in data.Colonies.Values)
            {
                if (!string.IsNullOrEmpty(colony.OwnerFaction) || string.IsNullOrEmpty(colony.Body))
                    continue;
                if (!string.IsNullOrEmpty(exceptColonyId) && colony.UniqueID == exceptColonyId)
                    continue;
                if (!TryFindBody(game, colony.System, colony.Body, out var system, out var body))
                    continue;

                if (!info.KnownSystems.Contains(system.ID))
                    info.KnownSystems.Add(system.ID);

                CreateFromBlueprint(game, playerFaction, playerSpecies, system, body, colony);
            }
        }

        /// <summary>
        /// Places colony blueprints that name an <see cref="ColonyBlueprint.OwnerFaction"/> and a body.
        /// Those are not player start options. The named faction is created if needed, and the stance
        /// string is stored both ways (Friendly or Allied is what makes them a trade partner).
        /// </summary>
        public static void PlaceOwnedColonies(Game game, ModDataStore data, Entity playerFaction, SpeciesBlueprint speciesBlueprint)
        {
            var created = new Dictionary<string, Entity>();
            var playerInfo = playerFaction.GetDataBlob<FactionInfoDB>();

            foreach (var colony in data.Colonies.Values)
            {
                if (string.IsNullOrEmpty(colony.OwnerFaction) || string.IsNullOrEmpty(colony.Body))
                    continue;
                if (!TryFindBody(game, colony.System, colony.Body, out var system, out var body))
                    continue;

                if (!created.TryGetValue(colony.OwnerFaction, out var faction))
                {
                    faction = FactionFactory.CreateBasicFaction(
                        game,
                        colony.OwnerFaction,
                        string.IsNullOrEmpty(colony.OwnerAbbreviation) ? colony.OwnerFaction : colony.OwnerAbbreviation,
                        colony.StartingFunds);
                    faction.FactionOwnerID = faction.Id;
                    created[colony.OwnerFaction] = faction;
                }

                var info = faction.GetDataBlob<FactionInfoDB>();
                if (!info.KnownSystems.Contains(system.ID))
                    info.KnownSystems.Add(system.ID);

                var species = SpeciesFactory.CreateFromBlueprint(system, speciesBlueprint);
                species.FactionOwnerID = faction.Id;
                info.Species.Add(species);

                // Installation designs look up resources, templates, and tech levels. A placed
                // faction does not run the player's long StartingItems list, so open those libraries.
                UnlockAll(info.Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID), info);
                UnlockAll(info.Data.LockedComponentTemplates.Keys, info);
                UnlockAll(info.Data.LockedIndustryTypes.Keys, info);
                UnlockAll(info.Data.LockedCargoTypes.Keys, info);
                UnlockAll(info.Data.LockedArmor.Keys, info);
                var lockedTechs = info.Data.LockedTechs.Keys.ToList();
                UnlockAll(lockedTechs, info);
                foreach (var id in lockedTechs)
                    info.Data.IncrementTechLevel(id);

                CreateFromBlueprint(game, faction, species, system, body, colony);

                if (Enum.TryParse<FactionStance>(colony.Stance, ignoreCase: true, out var stance))
                {
                    playerInfo.Stances[faction.Id] = stance;
                    info.Stances[playerFaction.Id] = stance;
                }
            }
        }

        static void WriteDepotPrices(Entity colony)
        {
            var policy = colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var existing)
                ? existing
                : new ColonyMarketPolicyDB();
            policy.Rows["methalox"] = Priced("methalox", CeresStart.FuelAsk, 0);
            policy.Rows["iron"] = Priced("iron", CeresStart.IronAsk, CeresStart.IronBid);
            policy.Rows["water"] = Priced("water", CeresStart.WaterAsk, 0);
            colony.SetDataBlob(policy);
        }

        static MarketPolicyRow Priced(string cargoId, decimal ask, decimal bid) => new()
        {
            CargoId = cargoId,
            Ask = ask,
            Bid = bid,
        };

        static void SeatStartingAdministrator(Game game, Entity faction, StarSystem system, Entity colony)
        {
            if (!colony.TryGetDataBlob<AdminSpaceDB>(out var adminSpace))
                return;

            AdminSpaceAbilityState? seat = null;
            foreach (var candidate in adminSpace.CommanderSeats)
            {
                if (candidate.CommanderID < 0)
                {
                    seat = candidate;
                    break;
                }
            }
            if (seat == null)
                return;

            var adminDB = CommanderFactory.CreateAdmin(game);
            adminDB.CommissionedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
            adminDB.RankedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365);
            var admin = CommanderFactory.Create(system, faction.Id, adminDB);
            seat.CommanderID = admin.Id;
            seat.Commander = adminDB;
            adminDB.AssignedTo = colony.Id;
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
            blobs.Add(new ActionQueueDB());
            blobs.Add(new MassVolumeDB());
            blobs.Add(new CargoStorageDB());
            blobs.Add(new PositionDB(pos, planetEntity));
            blobs.Add(new TeamsHousedDB());
            blobs.Add(new ComponentInstancesDB()); //installations get added to the componentInstancesDB
            blobs.Add(new InfrastructureDB()); //capacity gets summed from installations as they're added

            Entity colonyEntity = Entity.Create();
            colonyEntity.FactionOwnerID = factionEntity.Id;
            planetEntity.Manager.AddEntity(colonyEntity, blobs);
            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            factionInfo.Colonies.Add(colonyEntity);
            factionEntity.GetDataBlob<FactionOwnerDB>().SetOwned(colonyEntity);

            // Grant faction access to mineral data on this planet
            if (planetEntity.TryGetDataBlob<MineralsDB>(out var mineralsDB))
            {
                mineralsDB.GrantFactionAccess(factionInfo.FactionMask);
            }

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