using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

[TestFixture]
public class SaveRoundTripTests
{
    [Test]
    public void EarthColony_RoundTripsTheSavedGame()
    {
        var game = BuildEarth();
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        sol.UpdateActivityState();
        game.TimePulse.TimeStep();

        var saved = TakeSnapshot(game);
        string steelType = SteelJobType(game);
        Assert.That(saved.Queues[sol.ID], Is.Not.Empty);
        Assert.That(saved.MinerUnitsPerDay, Is.GreaterThan(0));
        Assert.That(steelType, Is.Not.Empty);

        string json = Game.Save(game);
        EntityIDGenerator.NextId = 0;
        Game.EntityIDCounter = 0;

        var loaded = Game.Load(json);
        var after = TakeSnapshot(loaded);

        Assert.That(after.GlobalDate, Is.EqualTo(saved.GlobalDate));
        Assert.That(after.SystemDates, Is.EqualTo(saved.SystemDates));
        Assert.That(after.Entities, Is.EqualTo(saved.Entities));
        Assert.That(after.LocalHumanFactionId, Is.EqualTo(saved.LocalHumanFactionId));
        Assert.That(after.EntityIdCounter, Is.EqualTo(saved.EntityIdCounter));
        Assert.That(after.NextEntityId, Is.EqualTo(saved.NextEntityId));
        Assert.That(after.Queues, Is.EqualTo(saved.Queues));
        Assert.That(after.MinerUnitsPerDay, Is.EqualTo(saved.MinerUnitsPerDay));
        Assert.That(after.JumpDestinations, Is.EqualTo(saved.JumpDestinations));
        Assert.That(SteelJobType(loaded), Is.EqualTo(steelType));

        foreach (var (managerId, entries) in after.QueueEntities)
        {
            var manager = loaded.GlobalManagerDictionary[managerId];
            foreach (var entity in entries)
            {
                Assert.That(manager.TryGetEntityById(entity.Id, out var canonical), Is.True, $"queue entity {entity.Id} in {managerId}");
                Assert.That(ReferenceEquals(entity, canonical), Is.True, $"queue entity {entity.Id} in {managerId} is a copy");
            }
        }

        var loadedSol = loaded.Systems.Single(s => s.ID == "system-sol");
        Assert.That(loadedSol.ActivityState, Is.EqualTo(SystemActivityState.Background));

        loaded.Settings.EnforceSingleThread = true;
        loaded.TimePulse.TimeStep();

        int created = Entity.Create().Id;
        Assert.That(saved.EntityIds, Does.Not.Contain(created));
    }

    [Test]
    public void EarthColony_LoadAcceptReadsTheHumanFaction()
    {
        var game = BuildEarth();
        var before = game.Factions[game.LocalHumanFactionId];
        Assert.That(before.TryGetDataBlob<FactionInfoDB>(out var savedInfo), Is.True);
        savedInfo!.KnownSystems.Add("system-sol");
        string json = Game.Save(game);

        string? known = null;
        var loaded = Game.Load(json, candidate =>
        {
            Assert.That(candidate.Factions.TryGetValue(candidate.LocalHumanFactionId, out var faction), Is.True);
            Assert.That(faction!.TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
            Assert.That(info!.KnownSystems, Is.EqualTo(new List<string> { "system-sol" }));
            known = info.KnownSystems[0];
            Assert.That(candidate.Systems.Any(system => system.ID == known), Is.True);
        });

        Assert.That(known, Is.EqualTo("system-sol"));
        Assert.That(loaded.Systems.Single(system => system.ID == "system-sol").ActivityState, Is.EqualTo(SystemActivityState.Background));
    }

    [Test]
    public void LoadAcceptThrow_RestoresTheIdCounters()
    {
        var game = BuildEarth();
        string json = Game.Save(game);
        Game.EntityIDCounter = 7;
        EntityIDGenerator.NextId = 11;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Game.Load(json, _ => throw new InvalidOperationException("rejected")));

        Assert.That(ex!.Message, Is.EqualTo("rejected"));
        Assert.That(Game.EntityIDCounter, Is.EqualTo(7));
        Assert.That(EntityIDGenerator.NextId, Is.EqualTo(11));
    }

    sealed class Snapshot
    {
        public DateTime GlobalDate;
        public Dictionary<string, DateTime> SystemDates = new();
        public Dictionary<string, string> Entities = new();
        public HashSet<int> EntityIds = new();
        public int LocalHumanFactionId;
        public int EntityIdCounter;
        public int NextEntityId;
        public Dictionary<string, List<string>> Queues = new();
        public Dictionary<string, List<Entity>> QueueEntities = new();
        public int MinerUnitsPerDay;
        public Dictionary<int, int> JumpDestinations = new();
    }

    static Snapshot TakeSnapshot(Game game)
    {
        var shot = new Snapshot
        {
            GlobalDate = game.TimePulse.GameGlobalDateTime,
            LocalHumanFactionId = game.LocalHumanFactionId,
            EntityIdCounter = Game.EntityIDCounter,
            NextEntityId = EntityIDGenerator.NextId,
            MinerUnitsPerDay = MinerUnits(game),
        };

        foreach (var system in game.Systems)
            shot.SystemDates[system.ID] = system.StarSysDateTime;

        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            var rows = new List<string>();
            foreach (var entity in manager.GetFilteredEntities(EntityFilter.Friendly, game.GameMasterFaction.Id).OrderBy(e => e.Id))
            {
                shot.EntityIds.Add(entity.Id);
                var types = manager.GetAllDataBlobTypesForEntity(entity.Id)
                    .Select(type => type.FullName ?? type.Name)
                    .OrderBy(name => name);
                rows.Add(entity.Id + ":" + string.Join(",", types));

                if (entity.TryGetDataBlob<JumpPointDB>(out var jump))
                    shot.JumpDestinations[entity.Id] = jump.DestinationId;
            }
            shot.Entities[manager.ManagerID] = string.Join("|", rows);

            var names = new List<string>();
            var entities = new List<Entity>();
            foreach (var entry in manager.ManagerSubpulses.InstanceProcessorsQueue)
            {
                var (processor, entity) = entry.Item;
                names.Add(processor + ":" + entity.Id);
                entities.Add(entity);
            }
            shot.Queues[manager.ManagerID] = names;
            shot.QueueEntities[manager.ManagerID] = entities;
        }

        return shot;
    }

    static string SteelJobType(Game game)
    {
        foreach (var manager in game.GlobalManagerDictionary.Values)
        {
            foreach (var ability in manager.GetAllDataBlobsOfType<IndustryAbilityDB>())
            {
                foreach (var line in ability.ProductionLines.Values)
                {
                    foreach (var job in line.Jobs)
                    {
                        if (job.ItemGuid == "stainless-steel" && !string.IsNullOrEmpty(job.TypeID))
                            return job.TypeID;
                    }
                }
            }
        }
        throw new InvalidOperationException("The stainless-steel job has no industry type.");
    }

    static int MinerUnits(Game game)
    {
        var faction = game.Factions[game.LocalHumanFactionId];
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            throw new InvalidOperationException("The human faction has no faction info.");
        return info.ComponentDesigns["default-design-asteroid-miner"].GetAttribute<AsteroidMinerAtb>().UnitsPerDay;
    }

    static Game BuildEarth()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        game.LocalHumanFactionId = player.Id;

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;

        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, store.Colonies["colony-earth"]);
        return game;
    }
}
