using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Technology;
using EngineHireRole = Pulsar4X.People.HireRole;
using ApiHireRole = Pulsar4X.Api.HireRole;

namespace Pulsar4X.Tests;

[TestFixture]
public class CivilianRecruitmentTests
{
    static Game NewGame(int seed)
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/sol-default/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false, MasterSeed = seed }, store);
        game.Settings.EnforceSingleThread = true;
        foreach (var sys in game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
        return game;
    }

    static Entity Colony(Game game, long population, out Entity faction, out Entity species, string name = "Recruit")
    {
        faction = FactionFactory.CreateFaction(game, name);
        var system = new StarSystem();
        system.Initialize(game, name + "-system", -1);
        system.SetActivityState(SystemActivityState.Stasis);
        var planet = Entity.Create();
        system.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB(name + " Prime"),
            new SystemBodyInfoDB { LengthOfDay = TimeSpan.Zero },
            MassVolumeDB.NewFromMassAndRadius_m(5.972e24, 6.371e6),
            new PositionDB(0, 0, 0),
        });
        species = Entity.Create(faction.Id);
        system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Human") });
        return ColonyFactory.CreateColony(faction, species, planet, population);
    }

    static Entity Faction(Entity colony) => colony.Manager!.Game.Factions[colony.FactionOwnerID];

    static long Pop(Entity colony) => colony.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

    static void AddChair(Entity colony)
    {
        if (!colony.TryGetDataBlob<AdminSpaceDB>(out var space))
        {
            space = new AdminSpaceDB();
            colony.SetDataBlob(space);
        }
        space.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "Colony Admin"));
    }

    // Slice 1: real scientists.

    [Test]
    public void Scientists_AreRolled_NotTheSamePlaceholder()
    {
        var game = NewGame(7);
        var colony = Colony(game, 1_000_000, out var faction, out _);
        var rolled = Enumerable.Range(0, 6).Select(_ => CommanderFactory.CreateScientist(faction, colony)).ToList();

        Assert.That(rolled.Select(s => s.Name).Distinct().Count(), Is.GreaterThan(1));
        foreach (var s in rolled)
        {
            Assert.That(s.Name, Is.Not.EqualTo("Augusta King"));
            Assert.That(s.MaxLabs, Is.InRange(CommanderFactory.ScientistMinLabs, CommanderFactory.ScientistMaxLabs));
            Assert.That(s.Bonuses.Keys, Has.Count.EqualTo(1));
            Assert.That(game.TechCategories.ContainsKey(s.Bonuses.Keys.Single()), Is.True);
        }
    }

    [Test]
    public void Scientists_SameSeed_SameScientist()
    {
        Scientist Roll(int seed)
        {
            var game = NewGame(seed);
            var colony = Colony(game, 1_000_000, out var faction, out _);
            return CommanderFactory.CreateScientist(faction, colony);
        }
        var a = Roll(99);
        var b = Roll(99);
        Assert.That(b.Name, Is.EqualTo(a.Name));
        Assert.That(b.MaxLabs, Is.EqualTo(a.MaxLabs));
        Assert.That(b.Bonuses.Keys, Is.EquivalentTo(a.Bonuses.Keys));
    }

    [Test]
    public void StartingColony_HasExactlyOneScientist()
    {
        var game = SolGame(out _);
        var luna = game.Factions.Values.Single(f => f.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        var colony = luna.GetDataBlob<FactionInfoDB>().Colonies.Single();
        Assert.That(colony.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science], Has.Count.EqualTo(1));
        Assert.That(luna.GetDataBlob<FactionTechDB>().AllScientists, Has.Count.EqualTo(1));
    }

    static Game SolGame(out Entity player)
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/sol-default/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        return game;
    }

    // Slice 2: the pool.

    [TestCase(99_999, 0)]
    [TestCase(100_000, 1)]
    [TestCase(999_999, 1)]
    [TestCase(1_000_000, 2)]
    [TestCase(100_000_000, 4)]
    [TestCase(8_000_000_000, 5)]
    public void PoolSize_GrowsWithPopulation_CappedAtFive(long pop, int size)
        => Assert.That(Recruitment.PoolSize(pop), Is.EqualTo(size));

    [Test]
    public void SmallColony_HasNoCandidates_AndSaysWhy()
    {
        var game = NewGame(1);
        var colony = Colony(game, 50_000, out _, out _);
        Assert.That(Recruitment.EmptyReason(colony), Is.EqualTo("Population below 100,000."));
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        Assert.That(colony.GetDataBlob<CandidatePoolDB>().Candidates, Is.Empty);
    }

    [Test]
    public void BeforeFirstRefresh_SaysCandidatesArriveNextUpdate()
    {
        var game = NewGame(1);
        var colony = Colony(game, 1_000_000, out _, out _);
        Assert.That(Recruitment.EmptyReason(colony), Is.EqualTo("First candidates arrive with the next population update."));
    }

    [Test]
    public void Refresh_FillsThenChurnsWithinLimits_AndTakesNobody()
    {
        var game = NewGame(3);
        var colony = Colony(game, 8_000_000_000, out _, out _);
        long before = Pop(colony);
        var now = colony.StarSysDateTime;

        Recruitment.Refresh(colony, now);
        var pool = colony.GetDataBlob<CandidatePoolDB>();
        Assert.That(pool.Candidates, Has.Count.EqualTo(5));

        for (int month = 1; month <= 24; month++)
        {
            var ids = pool.Candidates.Select(c => c.Id).ToList();
            now = now.AddDays(30);
            Recruitment.Refresh(colony, now);
            Assert.That(pool.LastRemoved, Is.InRange(0, Recruitment.MaxRemoved));
            Assert.That(pool.LastAdded, Is.InRange(0, Recruitment.MaxAdded));
            Assert.That(pool.Candidates.Count, Is.InRange(1, 5));
            Assert.That(pool.Candidates.Select(c => c.Id).Intersect(ids), Is.Not.Empty, "never a full turnover");
            Assert.That(pool.LastRefresh, Is.EqualTo(now));
        }
        Assert.That(Pop(colony), Is.EqualTo(before));
    }

    [Test]
    public void Refresh_SameSeed_SamePool()
    {
        List<string> Run(int seed)
        {
            var game = NewGame(seed);
            var colony = Colony(game, 50_000_000, out _, out _);
            var now = colony.StarSysDateTime;
            var log = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                Recruitment.Refresh(colony, now = now.AddDays(30));
                var pool = colony.GetDataBlob<CandidatePoolDB>();
                log.Add($"{pool.LastRemoved}/{pool.LastAdded}:" + string.Join(",", pool.Candidates.Select(c => $"{c.Name}|{c.Age}|{c.LabCategory}|{c.MaxLabs}")));
            }
            return log;
        }
        Assert.That(Run(42), Is.EqualTo(Run(42)));
    }

    [Test]
    public void Pool_SurvivesSaveAndLoad()
    {
        var game = NewGame(5);
        var colony = Colony(game, 1_000_000, out _, out _);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        var db = colony.GetDataBlob<CandidatePoolDB>();

        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new NonPublicResolver(),
        };
        var loaded = JsonConvert.DeserializeObject<CandidatePoolDB>(JsonConvert.SerializeObject(db, settings), settings)!;

        Assert.That(loaded.LastRefresh, Is.EqualTo(db.LastRefresh));
        Assert.That(loaded.Candidates.Select(c => (c.Id, c.Name, c.Age, c.SpeciesId, c.LabCategory, c.MaxLabs, c.JoinedOn)),
            Is.EqualTo(db.Candidates.Select(c => (c.Id, c.Name, c.Age, c.SpeciesId, c.LabCategory, c.MaxLabs, c.JoinedOn))));
    }

    // Slice 3: hiring.

    [Test]
    public void HireAsAdministrator_TakesOnePerson_AndAddsAnUnassignedCivilian()
    {
        var game = NewGame(11);
        var colony = Colony(game, 1_000_000, out var faction, out _);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        var candidate = colony.GetDataBlob<CandidatePoolDB>().Candidates[0];
        long before = Pop(colony);

        Assert.That(Recruitment.Hire(faction, colony, candidate.Id, EngineHireRole.Administrator, out var admin, out _), Is.Null);

        Assert.That(Pop(colony), Is.EqualTo(before - 1));
        Assert.That(colony.GetDataBlob<CandidatePoolDB>().Candidates.Any(c => c.Id == candidate.Id), Is.False);
        var db = admin!.GetDataBlob<CommanderDB>();
        Assert.That(db.Type, Is.EqualTo(CommanderTypes.Civilian));
        Assert.That(db.Name, Is.EqualTo(candidate.Name));
        Assert.That(db.AssignedTo, Is.EqualTo(-1));
        Assert.That(admin.FactionOwnerID, Is.EqualTo(faction.Id));
    }

    [Test]
    public void HireAsLabTechnician_HousesAScientistWithTheCandidatesStats()
    {
        var game = NewGame(12);
        var colony = Colony(game, 1_000_000, out var faction, out _);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        var candidate = colony.GetDataBlob<CandidatePoolDB>().Candidates[0];
        int scientists = faction.GetDataBlob<FactionTechDB>().AllScientists.Count;

        Assert.That(Recruitment.Hire(faction, colony, candidate.Id, EngineHireRole.LabTechnician, out _, out var sci), Is.Null);

        Assert.That(sci!.Name, Is.EqualTo(candidate.Name));
        Assert.That(sci.MaxLabs, Is.EqualTo(candidate.MaxLabs));
        Assert.That(sci.Bonuses.Keys.Single(), Is.EqualTo(candidate.LabCategory));
        Assert.That(faction.GetDataBlob<FactionTechDB>().AllScientists, Has.Count.EqualTo(scientists + 1));
        Assert.That(colony.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science], Does.Contain(sci));
    }

    [Test]
    public void Hire_FromAnotherFactionsColony_IsRefused()
    {
        var game = NewGame(13);
        var colony = Colony(game, 1_000_000, out _, out _);
        var other = FactionFactory.CreateFaction(game, "Other");
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[0].Id;
        long before = Pop(colony);

        Assert.That(Recruitment.Hire(other, colony, id, EngineHireRole.Administrator, out _, out _), Is.Not.Null);
        Assert.That(Pop(colony), Is.EqualTo(before));
    }

    [Test]
    public void HiredAdministrator_CanBeSeated_ByTheExistingOrder()
    {
        var game = NewGame(14);
        var colony = Colony(game, 1_000_000, out var faction, out _);
        AddChair(colony);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[0].Id;
        Recruitment.Hire(faction, colony, id, EngineHireRole.Administrator, out var admin, out _);

        Pulsar4X.People.Orders.AssignAdministratorOrder.Create(colony, admin!.Id, "Colony Admin").Execute(colony.StarSysDateTime);

        Assert.That(colony.GetDataBlob<AdminSpaceDB>().CommanderSeats[^1].CommanderID, Is.EqualTo(admin.Id));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));
    }

    // Slice 5: AI hiring.

    [Test]
    public void AutoHiringFaction_FillsSeatsThenLab_PlayerDoesNot()
    {
        var game = NewGame(15);
        var ai = Colony(game, 8_000_000_000, out var aiFaction, out _, "AI");
        var player = Colony(game, 8_000_000_000, out _, out _, "Player");
        aiFaction.GetDataBlob<FactionInfoDB>().AutoHires = true;
        AddChair(ai);
        AddChair(player);

        Recruitment.Refresh(ai, ai.StarSysDateTime);
        Recruitment.Refresh(player, player.StarSysDateTime);

        Assert.That(ai.GetDataBlob<AdminSpaceDB>().CommanderSeats.All(s => s.CommanderID >= 0), Is.True);
        Assert.That(player.GetDataBlob<AdminSpaceDB>().CommanderSeats.All(s => s.CommanderID < 0), Is.True);
        Assert.That(player.GetDataBlob<CandidatePoolDB>().Candidates, Has.Count.EqualTo(5));
    }

    [Test]
    public void LunaConcord_HiresForItself()
    {
        var game = SolGame(out var player);
        var luna = game.Factions.Values.Single(f => f.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        Assert.That(luna.GetDataBlob<FactionInfoDB>().AutoHires, Is.True);
        Assert.That(player.GetDataBlob<FactionInfoDB>().AutoHires, Is.False);
    }
}
// Slices 3 and 4 through the API.
[TestFixture]
public class CivilianRecruitmentApiTests : ApiTestBase
{
    Entity MakeColony(int factionId, long pop)
    {
        var faction = _game.Factions[factionId];
        var system = _game.Systems[0];
        var planet = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().First();
        var species = Entity.Create(factionId);
        system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Human") });
        var colony = ColonyFactory.CreateColony(faction, species, planet, pop);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        return colony;
    }

    [Test]
    public void View_ShowsOwnPoolAndFactionPicker_OnlyToTheOwner()
    {
        var session = Connect();
        var a = MakeColony(session.FactionId, 1_000_000);
        var b = MakeColony(session.FactionId, 8_000_000_000);
        int other = _game.Factions.Keys.First(id => id != session.FactionId);

        var view = _projector.ProjectEntity(a, session.FactionId).GetView<RecruitmentView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Candidates, Has.Count.EqualTo(2));
        Assert.That(view.EmptyReason, Is.Null);
        Assert.That(view.FactionCandidates, Has.Count.EqualTo(7));
        Assert.That(view.FactionCandidates.Count(c => c.ColonyId == b.Id), Is.EqualTo(5));
        Assert.That(view.FactionCandidates.All(c => c.Wage == Recruitment.PlaceholderWage && !string.IsNullOrEmpty(c.Location)), Is.True);

        Assert.That(_projector.ProjectEntity(a, other).GetView<RecruitmentView>(), Is.Null);
    }

    [Test]
    public void View_EmptyPool_ReportsReason()
    {
        var session = Connect();
        var small = MakeColony(session.FactionId, 10_000);
        var view = _projector.ProjectEntity(small, session.FactionId).GetView<RecruitmentView>()!;
        Assert.That(view.Candidates, Is.Empty);
        Assert.That(view.EmptyReason, Is.EqualTo("Population below 100,000."));
    }

    [Test]
    public void Command_HiresAsLabTechnician()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId, 1_000_000);
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[0].Id;

        var result = _server.SubmitCommand(session, new HireCandidateCommand(colony.Id, id, ApiHireRole.LabTechnician));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(colony.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science].OfType<Scientist>().Any(), Is.True);
    }

    [Test]
    public void Picker_HiresFromAnotherColony_AndSeatsThemAtThePost()
    {
        var session = Connect();
        var post = MakeColony(session.FactionId, 1_000_000);
        var source = MakeColony(session.FactionId, 8_000_000_000);
        var chair = new AdminSpaceDB();
        chair.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "Colony Admin"));
        if (post.HasDataBlob<AdminSpaceDB>()) post.GetDataBlob<AdminSpaceDB>().CommanderSeats.Add(chair.CommanderSeats[0]);
        else post.SetDataBlob(chair);
        var pick = _projector.ProjectEntity(post, session.FactionId).GetView<RecruitmentView>()!
            .FactionCandidates.First(c => c.ColonyId == source.Id);
        long sourcePop = source.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

        var result = _server.SubmitCommand(session, new HireCandidateCommand(pick.ColonyId, pick.Id, ApiHireRole.Administrator, post.Id));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(source.GetDataBlob<ColonyInfoDB>().Population.Values.Sum(), Is.EqualTo(sourcePop - 1));
        var seat = post.GetDataBlob<AdminSpaceDB>().CommanderSeats.First(s => s.SeatType >= AdminLevel.Colony);
        Assert.That(seat.CommanderID, Is.GreaterThanOrEqualTo(0));
        Assert.That(seat.Commander!.Name, Is.EqualTo(pick.Name));
    }

    [Test]
    public void Command_ForAnotherFactionsColony_IsRejected()
    {
        var session = Connect();
        int other = _game.Factions.Keys.First(id => id != session.FactionId);
        var colony = MakeColony(other, 1_000_000);
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[0].Id;

        var result = _server.SubmitCommand(session, new HireCandidateCommand(colony.Id, id, ApiHireRole.Administrator));
        Assert.That(result.Accepted, Is.False);
    }
}
