using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using GameEngine.People;
using Pulsar4X.Api;
using Pulsar4X.Galaxy;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using EngineHireRole = Pulsar4X.People.HireRole;

namespace Pulsar4X.Tests;

/// <summary>
/// Portraits phase 2, engine side: species data carries an optional PortraitPack, and every view that shows a person carries the
/// pack id beside the seed and role. The pack is derived from species data when a view is built, never saved per person.
/// </summary>
[TestFixture]
public class PortraitSpeciesTests : ApiTestBase
{
    Entity NewSpecies(int factionId, string? pack, bool addToFaction = false)
    {
        var db = new SpeciesDB(1, 0.1, 2, 1, 0.4, 4, 14, -15, 45);
        if (pack != null)
            db.PortraitPack = pack;
        var species = Entity.Create(factionId);
        _game.GlobalManager.AddEntity(species, new List<BaseDataBlob> { new NameDB("Species " + (pack ?? "none")), db });
        if (addToFaction)
            _game.Factions[factionId].GetDataBlob<FactionInfoDB>().Species.Add(species);
        return species;
    }

    Entity MakeColony(int factionId, Entity species)
    {
        var faction = _game.Factions[factionId];
        var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().First();
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

    Entity Person(int factionId, int speciesId = -1, CommanderTypes type = CommanderTypes.Civilian)
    {
        var person = CommanderFactory.Create(_game.GlobalManager, factionId, new CommanderDB("Pat", 1, type));
        person.GetDataBlob<CommanderDB>().SpeciesId = speciesId;
        return person;
    }

    string RosterPack(int factionId, Entity person)
        => _projector.ProjectCommanders(factionId)!.Single(c => c.Id == person.Id).PortraitSpecies;

    // ----- the data -----

    static SpeciesBlueprint Blueprint(string json) => JsonConvert.DeserializeObject<SpeciesBlueprint>(json)!;

    const string Ranges = "\"Gravity\":{\"Ideal\":1,\"Minimum\":0.1,\"Maximum\":2},\"Pressure\":{\"Ideal\":1,\"Minimum\":0.4,\"Maximum\":4},\"Temperature\":{\"Ideal\":14,\"Minimum\":-15,\"Maximum\":45}";

    [Test]
    public void APortraitPackAbsentFromData_DefaultsToHuman()
    {
        var bare = Blueprint("{\"UniqueID\":\"s-bare\",\"Name\":\"Bare\"," + Ranges + "}");
        Assert.That(bare.PortraitPack, Is.EqualTo("human"));
        Assert.That(new SpeciesDB().PortraitPack, Is.EqualTo("human"));

        var entity = SpeciesFactory.CreateFromBlueprint(_game.Systems[0], bare);
        Assert.That(entity.GetDataBlob<SpeciesDB>().PortraitPack, Is.EqualTo("human"));
    }

    [Test]
    public void APortraitPackInData_ReachesTheSpeciesEntity_AndItsClone()
    {
        var lizard = Blueprint("{\"UniqueID\":\"s-liz\",\"Name\":\"Lizard\",\"PortraitPack\":\"lizard\"," + Ranges + "}");
        var db = SpeciesFactory.CreateFromBlueprint(_game.Systems[0], lizard).GetDataBlob<SpeciesDB>();
        Assert.That(db.PortraitPack, Is.EqualTo("lizard"));
        Assert.That(((SpeciesDB)db.Clone()).PortraitPack, Is.EqualTo("lizard"));

        var blank = Blueprint("{\"UniqueID\":\"s-blank\",\"Name\":\"Blank\",\"PortraitPack\":\"\"," + Ranges + "}");
        Assert.That(SpeciesFactory.CreateFromBlueprint(_game.Systems[0], blank).GetDataBlob<SpeciesDB>().PortraitPack, Is.EqualTo("human"),
            "a blank pack means the default");
    }

    [Test]
    public void TheShippedSpeciesData_NamesItsPack_AndTheLoaderKeepsIt()
    {
        var species = JArray.Parse(File.ReadAllText("Data/scenarios/warp-tech-start/species.json"));
        Assert.That(species, Is.Not.Empty);
        foreach (var entry in species)
            Assert.That((string?)entry["Payload"]!["PortraitPack"], Is.EqualTo("human"), (string?)entry["Payload"]!["UniqueID"]);

        var store = new ModDataStore();
        var loader = new ModLoader();
        loader.LoadModManifest("Data/basemod/modInfo.json", store);
        loader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        Assert.That(store.Species.Values.Select(s => s.PortraitPack), Has.All.EqualTo("human"));
    }

    // ----- the projection -----

    [Test]
    public void APersonsOwnSpecies_PicksThePack()
    {
        var session = Connect();
        var human = NewSpecies(session.FactionId, "human", addToFaction: true);
        var alien = NewSpecies(session.FactionId, "alien");
        var person = Person(session.FactionId, alien.Id);

        Assert.That(RosterPack(session.FactionId, person), Is.EqualTo("alien"), "their own species beats the faction's");
        Assert.That(RosterPack(session.FactionId, Person(session.FactionId, human.Id)), Is.EqualTo("human"));
    }

    [Test]
    public void ANewPackOnTheSpecies_ShowsAtOnce_BecauseItIsNotSavedPerPerson()
    {
        var session = Connect();
        var species = NewSpecies(session.FactionId, "human", addToFaction: true);
        var person = Person(session.FactionId, species.Id);
        Assert.That(RosterPack(session.FactionId, person), Is.EqualTo("human"));

        species.GetDataBlob<SpeciesDB>().PortraitPack = "scaled";
        Assert.That(RosterPack(session.FactionId, person), Is.EqualTo("scaled"));
        Assert.That(typeof(CommanderDB).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Select(p => p.Name), Has.None.Contains("Pack"), "no pack is stored on the person");
    }

    [Test]
    public void APersonWithNoSpecies_GetsTheFactionsPrimarySpeciesPack()
    {
        var session = Connect();
        _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Species.Clear(); // the test faction starts with a species of its own
        var primary = NewSpecies(session.FactionId, "alien", addToFaction: true);
        NewSpecies(session.FactionId, "human", addToFaction: true); // the second species is not the primary
        var person = Person(session.FactionId);

        Assert.That(person.GetDataBlob<CommanderDB>().SpeciesId, Is.EqualTo(-1));
        Assert.That(RosterPack(session.FactionId, person), Is.EqualTo("alien"));

        // A species id that no longer resolves falls back the same way.
        var orphan = Person(session.FactionId, 987_654);
        Assert.That(RosterPack(session.FactionId, orphan), Is.EqualTo("alien"));
        Assert.That(primary.IsValid, Is.True);
    }

    [Test]
    public void WithNoSpeciesAnywhere_ThePackIsHuman()
    {
        var session = Connect();
        _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Species.Clear();
        Assert.That(RosterPack(session.FactionId, Person(session.FactionId)), Is.EqualTo("human"));

        // A species with no pack on it (as an old save would have) reads human too.
        var plain = NewSpecies(session.FactionId, null, addToFaction: true);
        Assert.That(RosterPack(session.FactionId, Person(session.FactionId)), Is.EqualTo("human"));
        Assert.That(plain.GetDataBlob<SpeciesDB>().PortraitPack, Is.EqualTo("human"));
    }

    [Test]
    public void ACandidateShowsItsSpeciesPack_AndTheHiredPersonKeepsIt()
    {
        var session = Connect();
        var alien = NewSpecies(session.FactionId, "alien");
        NewSpecies(session.FactionId, "human", addToFaction: true);
        var colony = MakeColony(session.FactionId, alien);

        var recruitment = _projector.ProjectEntity(colony, session.FactionId).GetView<RecruitmentView>()!;
        Assert.That(recruitment.Candidates, Is.Not.Empty);
        Assert.That(recruitment.Candidates.Select(c => c.PortraitSpecies), Has.All.EqualTo("alien"));
        Assert.That(recruitment.FactionCandidates.Select(c => c.PortraitSpecies), Has.All.EqualTo("alien"));

        var candidate = recruitment.Candidates[0];
        Assert.That(Recruitment.Hire(_game.Factions[session.FactionId], colony, candidate.Id, EngineHireRole.Administrator,
            out var admin, out _), Is.Null);
        Assert.That(admin!.GetDataBlob<CommanderDB>().SpeciesId, Is.EqualTo(alien.Id));
        Assert.That(RosterPack(session.FactionId, admin), Is.EqualTo("alien"), "the faction primary is human, so this is the person's own");
    }

    [Test]
    public void TheColonyViews_CarryThePackBesideTheSeedAndRole()
    {
        var session = Connect();
        var alien = NewSpecies(session.FactionId, "alien");
        NewSpecies(session.FactionId, "human", addToFaction: true);
        var colony = MakeColony(session.FactionId, alien);
        var head = Person(session.FactionId, alien.Id);
        var chair = Person(session.FactionId); // no species of their own: the faction's primary (human)

        Assert.That(_server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, head.Id)).Accepted, Is.True);
        Assert.That(_server.SubmitCommand(session, new AssignAdministratorCommand(colony.Id, chair.Id)).Accepted, Is.True);

        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((view.IndustryHeadPortraitRole, view.IndustryHeadPortraitSpecies), Is.EqualTo(("industry-head", "alien")));
        Assert.That((view.AdministratorPortraitRole, view.AdministratorPortraitSpecies), Is.EqualTo(("administrator", "human")));
        var industry = _projector.ProjectEntity(colony, session.FactionId).GetView<IndustryView>();
        if (industry != null)
            Assert.That(industry.IndustryHeadPortraitSpecies, Is.EqualTo("alien"));
    }

    [Test]
    public void AScientistInALab_CarriesThePack()
    {
        var session = Connect();
        var alien = NewSpecies(session.FactionId, "alien");
        var scientist = Person(session.FactionId, alien.Id, CommanderTypes.Scientist);
        var research = _projector.ProjectResearch(session.FactionId)!;
        Assert.That(research.Scientists.Single(s => s.Id == scientist.Id).PortraitRole, Is.EqualTo("scientist"));
        Assert.That(RosterPack(session.FactionId, scientist), Is.EqualTo("alien"));
    }

    // ----- a post that points at nothing -----

    [Test]
    public void ADanglingChair_ProjectsAsAnEmptyPost_NotAnIdDerivedFace()
    {
        var session = Connect();
        NewSpecies(session.FactionId, "human", addToFaction: true);
        var colony = MakeColony(session.FactionId, NewSpecies(session.FactionId, "human"));
        var seat = colony.GetDataBlob<AdminSpaceDB>().CommanderSeats.First(s => s.SeatType >= AdminLevel.Colony);
        seat.CommanderID = 876_543; // nobody has this id

        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((view.AdministratorPortraitSeed, view.AdministratorPortraitRole, view.AdministratorPortraitSpecies), Is.EqualTo((0, "", "")));
        Assert.That(view.AdministratorId, Is.EqualTo(876_543), "the post still names the missing id");
    }

    [Test]
    public void ADeadChairHolder_ProjectsAsAnEmptyPost()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId, NewSpecies(session.FactionId, "human", addToFaction: true));
        var chair = Person(session.FactionId);
        Assert.That(_server.SubmitCommand(session, new AssignAdministratorCommand(colony.Id, chair.Id)).Accepted, Is.True);
        Assert.That(_projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!.AdministratorPortraitRole, Is.EqualTo("administrator"));

        chair.Destroy();
        _game.GlobalManager.RemoveTaggedEntitys(); // the removal the next pulse would do
        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((view.AdministratorPortraitSeed, view.AdministratorPortraitRole, view.AdministratorPortraitSpecies), Is.EqualTo((0, "", "")));
    }

    [Test]
    public void AnEmptyPost_HasNoPack()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId, NewSpecies(session.FactionId, "human", addToFaction: true));
        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That((view.AdministratorPortraitSpecies, view.IndustryHeadPortraitSpecies), Is.EqualTo(("", "")));
    }
}
