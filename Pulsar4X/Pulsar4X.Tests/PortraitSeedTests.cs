using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Technology;
using EngineHireRole = Pulsar4X.People.HireRole;

namespace Pulsar4X.Tests;

/// <summary>
/// Portraits, engine side: every person has a portrait seed (set once, saved, kept on hire) and every view that shows a person
/// carries it with a role that follows their post. No image data lives in the engine.
/// </summary>
[TestFixture]
public class PortraitSeedTests : ApiTestBase
{
    Entity MakeColony(int factionId, string name = "Portrait Colony")
    {
        var faction = _game.Factions[factionId];
        var system = _game.Systems[0];
        var planet = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().First();
        var species = Entity.Create(factionId);
        system.AddEntity(species, new List<BaseDataBlob> { new Pulsar4X.Names.NameDB("Human") });
        var colony = ColonyFactory.CreateColony(faction, species, planet, 1_000_000);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        if (!colony.TryGetDataBlob<AdminSpaceDB>(out var space))
        {
            space = new AdminSpaceDB();
            colony.SetDataBlob(space);
        }
        space.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "City Hall"));
        return colony;
    }

    Entity Hire(Entity colony, int index = 0)
    {
        var faction = _game.Factions[colony.FactionOwnerID];
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[index].Id;
        Assert.That(Recruitment.Hire(faction, colony, id, EngineHireRole.Administrator, out var admin, out _), Is.Null);
        return admin!;
    }

    static Entity Person(Game game, int factionId, CommanderTypes type = CommanderTypes.Civilian, string name = "Pat")
        => CommanderFactory.Create(game.GlobalManager, factionId, new CommanderDB(name, 1, type));

    Scientist NewScientist(Entity colony)
        => CommanderFactory.CreateScientist(_game.Factions[colony.FactionOwnerID], colony, "Lab Tech", 33, "", 20);

    // A candidate we control, added to the pool.
    static Candidate AddCandidate(Entity colony, int seed)
    {
        var candidate = new Candidate { Id = Game.GetEntityID(), Name = "Extra", Age = 30, PortraitSeed = seed, JoinedOn = colony.StarSysDateTime };
        colony.GetDataBlob<CandidatePoolDB>().Candidates.Add(candidate);
        return candidate;
    }

    static CommanderSnapshot Roster(GameProjector projector, int factionId, Entity person)
        => projector.ProjectCommanders(factionId)!.Single(c => c.Id == person.Id);

    // ----- the seed -----

    [Test]
    public void EveryCreatedPerson_HasANonZeroSeed_AndNoTwoMatch()
    {
        var session = Connect();
        var people = new[]
        {
            Person(_game, session.FactionId, CommanderTypes.Civilian, "A"),
            Person(_game, session.FactionId, CommanderTypes.Navy, "B"),
            Person(_game, session.FactionId, CommanderTypes.Scientist, "C"),
            CommanderFactory.Create(_game.GlobalManager, session.FactionId, CommanderFactory.CreateShipCaptain(_game)),
        };

        var seeds = people.Select(p => p.GetDataBlob<CommanderDB>().PortraitSeed).ToList();

        Assert.That(seeds, Has.None.EqualTo(0));
        Assert.That(seeds.Distinct().Count(), Is.EqualTo(seeds.Count));
    }

    [Test]
    public void ASeedSetBeforeCreation_IsKept()
    {
        var session = Connect();
        var person = CommanderFactory.Create(_game.GlobalManager, session.FactionId,
            new CommanderDB("Kept", 1, CommanderTypes.Civilian) { PortraitSeed = 424242 });

        Assert.That(person.GetDataBlob<CommanderDB>().PortraitSeed, Is.EqualTo(424242));
    }

    [Test]
    public void ANewLabTechnician_AndEveryNewCandidate_HaveSeeds()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var scientist = NewScientist(colony);

        var candidates = colony.GetDataBlob<CandidatePoolDB>().Candidates;
        Assert.That(candidates, Is.Not.Empty);
        Assert.That(candidates.Select(c => c.PortraitSeed), Has.None.EqualTo(0));
        Assert.That(scientist.PortraitSeed, Is.Not.EqualTo(0));
        var tech = _game.Factions[session.FactionId].GetDataBlob<FactionTechDB>();
        Assert.That(tech.AllScientists, Is.Not.Empty);
        Assert.That(tech.AllScientists.Select(s => s.scientist.PortraitSeed), Has.None.EqualTo(0));
    }

    // A fresh game with a chosen master seed, for the determinism tests.
    static (Game game, Entity faction) NewGame(int masterSeed)
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false, MasterSeed = masterSeed }, store);
        game.Settings.EnforceSingleThread = true;
        return (game, FactionFactory.CreateFaction(game, "Portraits-" + masterSeed));
    }

    static List<int> Seeds(Game game, Entity faction, int count)
        => Enumerable.Range(0, count)
            .Select(i => Person(game, faction.Id, CommanderTypes.Civilian, "P" + i).GetDataBlob<CommanderDB>().PortraitSeed)
            .ToList();

    [Test]
    public void TheSameMasterSeed_GivesTheSameSeeds_InTheSameOrder_AndAnotherSeedDoesNot()
    {
        var (a, factionA) = NewGame(1001);
        var (b, factionB) = NewGame(1001);
        var (c, factionC) = NewGame(2002);

        var seedsA = Seeds(a, factionA, 5);
        var seedsB = Seeds(b, factionB, 5);
        var seedsC = Seeds(c, factionC, 5);

        Assert.That(seedsB, Is.EqualTo(seedsA));
        Assert.That(seedsC, Is.Not.EqualTo(seedsA));
        Assert.That(seedsA.Distinct().Count(), Is.EqualTo(5));
    }

    [Test]
    public void DrawingSeeds_DoesNotMoveTheGameRandom()
    {
        var (a, factionA) = NewGame(3003);
        var (b, factionB) = NewGame(3003);

        Seeds(a, factionA, 10); // b creates nobody

        Assert.That(a.RNG.Next(), Is.EqualTo(b.RNG.Next()), "a seed uses its own stream, so the game's sequence is untouched");
        Assert.That(factionB, Is.Not.Null);
    }

    // ----- save and load -----

    static Game SaveAndLoad(Game game)
    {
        string json = Game.Save(game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);
        return loaded;
    }

    static Entity Find(Game game, int id)
    {
        foreach (var manager in game.GlobalManagerDictionary.Values)
            if (manager.TryGetGlobalEntityById(id, out var e))
                return e;
        throw new AssertionException("entity " + id + " not found after load");
    }

    [Test]
    public void TheSeeds_SurviveSaveAndLoad_AndTheStreamContinues()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = Hire(colony);
        var candidate = AddCandidate(colony, 777_001);
        var scientist = NewScientist(colony);

        var loaded = SaveAndLoad(_game);

        Assert.That(Find(loaded, admin.Id).GetDataBlob<CommanderDB>().PortraitSeed,
            Is.EqualTo(admin.GetDataBlob<CommanderDB>().PortraitSeed));
        var loadedPool = Find(loaded, colony.Id).GetDataBlob<CandidatePoolDB>().Candidates.Single(c => c.Id == candidate.Id);
        Assert.That(loadedPool.PortraitSeed, Is.EqualTo(candidate.PortraitSeed));
        var loadedScientist = loaded.Factions[session.FactionId].GetDataBlob<FactionTechDB>().AllScientists
            .Single(s => s.scientist.ID == scientist.ID).scientist;
        Assert.That(loadedScientist.PortraitSeed, Is.EqualTo(scientist.PortraitSeed));

        Assert.That(loaded.GlobalManager.NextPortraitSeed(), Is.EqualTo(_game.GlobalManager.NextPortraitSeed()),
            "the draw counter is saved, so the next person gets the seed they would have had");
    }

    [Test]
    public void AnOldSave_WithNoSeeds_GetsSeedsDerivedFromIds_TheSameOnEveryLoad()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = Hire(colony);
        var candidate = AddCandidate(colony, 777_002);
        var scientist = NewScientist(colony);
        admin.GetDataBlob<CommanderDB>().PortraitSeed = 0; // as saved before portraits existed
        candidate.PortraitSeed = 0;
        scientist.PortraitSeed = 0;
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;

        var first = Game.Load(json);
        var second = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        foreach (var loaded in new[] { first, second })
        {
            Assert.That(Find(loaded, admin.Id).GetDataBlob<CommanderDB>().PortraitSeed, Is.EqualTo(Portraits.FromId(admin.Id)));
            Assert.That(Find(loaded, colony.Id).GetDataBlob<CandidatePoolDB>().Candidates.Single(c => c.Id == candidate.Id).PortraitSeed,
                Is.EqualTo(Portraits.FromId(candidate.Id)));
            Assert.That(loaded.Factions[session.FactionId].GetDataBlob<FactionTechDB>().AllScientists
                .Single(s => s.scientist.ID == scientist.ID).scientist.PortraitSeed, Is.EqualTo(Portraits.FromId(scientist.ID)));
        }
        Assert.That(Portraits.FromId(admin.Id), Is.Not.EqualTo(0));
        Assert.That(Portraits.FromId(1), Is.Not.EqualTo(Portraits.FromId(2)));
    }

    [Test]
    public void AViewOfAnOldPerson_NeverShowsSeedZero()
    {
        var session = Connect();
        var person = Person(_game, session.FactionId);
        person.GetDataBlob<CommanderDB>().PortraitSeed = 0;

        Assert.That(Roster(_projector, session.FactionId, person).PortraitSeed, Is.EqualTo(Portraits.FromId(person.Id)));
    }

    // ----- hiring keeps the face -----

    [Test]
    public void AHiredAdministrator_HasTheSeedTheCandidateHad()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var viewed = _projector.ProjectEntity(colony, session.FactionId).GetView<RecruitmentView>()!.Candidates[0];
        Assert.That(viewed.PortraitRole, Is.EqualTo("candidate"));
        Assert.That(viewed.PortraitSeed, Is.Not.EqualTo(0));

        var admin = Hire(colony, 0);

        Assert.That(admin.GetDataBlob<CommanderDB>().PortraitSeed, Is.EqualTo(viewed.PortraitSeed));
        Assert.That(Roster(_projector, session.FactionId, admin).PortraitSeed, Is.EqualTo(viewed.PortraitSeed));
    }

    [Test]
    public void AHiredLabTechnician_HasTheSeedTheCandidateHad()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var faction = _game.Factions[session.FactionId];
        var candidate = colony.GetDataBlob<CandidatePoolDB>().Candidates[0];
        int seed = candidate.PortraitSeed;

        Assert.That(Recruitment.Hire(faction, colony, candidate.Id, EngineHireRole.LabTechnician, out _, out var scientist), Is.Null);

        Assert.That(scientist!.PortraitSeed, Is.EqualTo(seed));
    }

    [Test]
    public void ACandidateFromAnOldSave_KeepsItsDerivedSeedOnHire()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var candidate = colony.GetDataBlob<CandidatePoolDB>().Candidates[0];
        candidate.PortraitSeed = 0;
        int shown = _projector.ProjectEntity(colony, session.FactionId).GetView<RecruitmentView>()!.Candidates.Single(c => c.Id == candidate.Id).PortraitSeed;
        Assert.That(shown, Is.EqualTo(Portraits.FromId(candidate.Id)));

        var admin = Hire(colony, 0);

        Assert.That(admin.GetDataBlob<CommanderDB>().PortraitSeed, Is.EqualTo(shown));
    }

    // ----- roles follow the post -----

    [Test]
    public void ARoleFollowsThePost_Civilian_IndustryHead_Civilian_Administrator()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = Hire(colony);
        var other = Hire(colony);
        int seed = admin.GetDataBlob<CommanderDB>().PortraitSeed;

        Assert.That(Roster(_projector, session.FactionId, admin).PortraitRole, Is.EqualTo("civilian"), "hired, not seated");

        Assert.That(_server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id)).Accepted, Is.True);
        var seated = Roster(_projector, session.FactionId, admin);
        Assert.That(seated.PortraitRole, Is.EqualTo("industry-head"));
        Assert.That(seated.PortraitSeed, Is.EqualTo(seed), "the face does not change with the post");
        var colonyView = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((colonyView.IndustryHeadPortraitSeed, colonyView.IndustryHeadPortraitRole), Is.EqualTo((seed, "industry-head")));
        Assert.That(colonyView.AdministratorPortraitSeed, Is.EqualTo(0), "no chair holder");
        Assert.That(colonyView.AdministratorPortraitRole, Is.EqualTo(""));
        var industry = _projector.ProjectEntity(colony, session.FactionId).GetView<IndustryView>();
        if (industry != null)
            Assert.That((industry.IndustryHeadPortraitSeed, industry.IndustryHeadPortraitRole), Is.EqualTo((seed, "industry-head")));

        Assert.That(_server.SubmitCommand(session, new UnseatIndustryHeadCommand(colony.Id)).Accepted, Is.True);
        Assert.That(Roster(_projector, session.FactionId, admin).PortraitRole, Is.EqualTo("civilian"), "unseated: a civilian again");
        var emptied = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((emptied.IndustryHeadPortraitSeed, emptied.IndustryHeadPortraitRole), Is.EqualTo((0, "")));

        Assert.That(_server.SubmitCommand(session, new AssignAdministratorCommand(colony.Id, other.Id)).Accepted, Is.True);
        Assert.That(Roster(_projector, session.FactionId, other).PortraitRole, Is.EqualTo("administrator"));
        var chair = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((chair.AdministratorPortraitSeed, chair.AdministratorPortraitRole),
            Is.EqualTo((other.GetDataBlob<CommanderDB>().PortraitSeed, "administrator")));
    }

    [Test]
    public void OfficersAreCaptains_AndScientistsAreScientists()
    {
        var session = Connect();
        var navy = Person(_game, session.FactionId, CommanderTypes.Navy, "Officer");
        var ground = Person(_game, session.FactionId, CommanderTypes.Ground, "Sergeant");
        var scientist = Person(_game, session.FactionId, CommanderTypes.Scientist, "Doctor");

        Assert.That(Roster(_projector, session.FactionId, navy).PortraitRole, Is.EqualTo("captain"));
        Assert.That(Roster(_projector, session.FactionId, ground).PortraitRole, Is.EqualTo("captain"));
        Assert.That(Roster(_projector, session.FactionId, scientist).PortraitRole, Is.EqualTo("scientist"));
        Assert.That(_projector.ProjectResearch(session.FactionId)!.Scientists.Single(s => s.Id == scientist.Id).PortraitRole,
            Is.EqualTo("scientist"));
    }

    [Test]
    public void TheRoleStrings_AreExactlyTheSpecs()
    {
        Assert.That(PortraitRoles.All, Is.EqualTo(new[]
        {
            "captain", "administrator", "industry-head", "scientist", "lab-technician", "civilian", "candidate",
        }));
        Assert.That(PortraitRoles.LabTechnician, Is.EqualTo("lab-technician"));
    }
}
