using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.People.Orders;
using EngineHireRole = Pulsar4X.People.HireRole;

namespace Pulsar4X.Tests;

/// <summary>
/// Civilian industry plan, slice 1: the colony's industry-head seat. A hired administrator can be seated;
/// the seat is apart from the city-hall chair and one person cannot hold both.
/// </summary>
[TestFixture]
public class IndustrySeatTests : ApiTestBase
{
    Entity MakeColony(int factionId, bool withChair = true, string name = "Seat Colony")
    {
        var faction = _game.Factions[factionId];
        var system = _game.Systems[0];
        var planet = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().First();
        var species = Entity.Create(factionId);
        system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Human") });
        var colony = ColonyFactory.CreateColony(faction, species, planet, 1_000_000);
        Recruitment.Refresh(colony, colony.StarSysDateTime);
        if (withChair)
        {
            if (!colony.TryGetDataBlob<AdminSpaceDB>(out var space))
            {
                space = new AdminSpaceDB();
                colony.SetDataBlob(space);
            }
            space.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "City Hall"));
        }
        return colony;
    }

    // A real hire from the colony's candidate pool: an unassigned civilian administrator.
    Entity HireAdmin(Entity colony)
    {
        var faction = _game.Factions[colony.FactionOwnerID];
        var id = colony.GetDataBlob<CandidatePoolDB>().Candidates[0].Id;
        Assert.That(Recruitment.Hire(faction, colony, id, EngineHireRole.Administrator, out var admin, out _), Is.Null);
        Assert.That(admin!.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        return admin;
    }

    static int Head(Entity colony) => colony.TryGetDataBlob<IndustrySeatDB>(out var seat) ? seat.HeadId : -1;

    static int ChairHolder(Entity colony)
        => colony.GetDataBlob<AdminSpaceDB>().CommanderSeats.First(s => s.SeatType >= AdminLevel.Colony).CommanderID;

    [Test]
    public void HiredAdmin_CanBeSeated_AsIndustryHead()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);

        var result = _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Head(colony), Is.EqualTo(admin.Id));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));
        Assert.That(ChairHolder(colony), Is.EqualTo(-1), "the industry seat is not the city-hall chair");
        Assert.That(colony.GetDataBlob<ShipLogDB>().Entries.Last().Text, Does.Contain("took the industry seat"));

        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That(view.IndustryHeadId, Is.EqualTo(admin.Id));
        Assert.That(view.IndustryHeadName, Is.EqualTo(admin.GetName(session.FactionId)));
        Assert.That(view.AdministratorId, Is.EqualTo(-1), "the chair view is unchanged");

        var roster = _projector.ProjectCommanders(session.FactionId)!.Single(c => c.Id == admin.Id);
        Assert.That(roster.IsAssigned, Is.True, "a seated head is no longer an unassigned civilian");
    }

    [Test]
    public void Seat_AndUnseatOrders_Work()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);

        SeatIndustryHeadOrder.Create(colony, admin.Id).Execute(colony.StarSysDateTime);
        Assert.That(Head(colony), Is.EqualTo(admin.Id));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));

        UnseatIndustryHeadOrder.Create(colony).Execute(colony.StarSysDateTime);
        Assert.That(Head(colony), Is.EqualTo(-1));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
    }

    [Test]
    public void Unseating_EmptiesTheSeat_AndFreesThePerson()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);
        _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id));

        var result = _server.SubmitCommand(session, new UnseatIndustryHeadCommand(colony.Id));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Head(colony), Is.EqualTo(-1));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        Assert.That(colony.GetDataBlob<ShipLogDB>().Entries.Last().Text, Does.Contain("left the industry seat"));
        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<ColonyView>()!;
        Assert.That(view.IndustryHeadId, Is.EqualTo(-1));
        Assert.That(view.IndustryHeadName, Is.Null);

        // Unseating an empty seat is fine, and the freed person can be seated again.
        Assert.That(_server.SubmitCommand(session, new UnseatIndustryHeadCommand(colony.Id)).Accepted, Is.True);
        Assert.That(_server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id)).Accepted, Is.True);
        Assert.That(Head(colony), Is.EqualTo(admin.Id));
    }

    [Test]
    public void TheSeat_SurvivesSaveAndLoad()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);
        _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id));

        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(colony.Id, out var loadedColony), Is.True);
        Assert.That(loadedColony.GetDataBlob<IndustrySeatDB>().HeadId, Is.EqualTo(admin.Id));
        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(admin.Id, out var loadedAdmin), Is.True);
        Assert.That(loadedAdmin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));

        var loadedProjector = new Pulsar4X.Engine.Api.GameProjector(loaded);
        var view = loadedProjector.ProjectEntity(loadedColony, session.FactionId).GetView<ColonyView>()!;
        Assert.That(view.IndustryHeadId, Is.EqualTo(admin.Id));
        Assert.That(view.IndustryHeadName, Is.EqualTo(admin.GetName(session.FactionId)));
    }

    [Test]
    public void ChairHolder_CannotBeSeatedAsIndustryHead()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var other = MakeColony(session.FactionId, name: "Other");
        var admin = HireAdmin(colony);
        Assert.That(_server.SubmitCommand(session, new AssignAdministratorCommand(colony.Id, admin.Id)).Accepted, Is.True);

        foreach (var target in new[] { colony, other })
        {
            var refused = _server.SubmitCommand(session, new SeatIndustryHeadCommand(target.Id, admin.Id));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason, Does.Contain("holds a city-hall chair"));
            Assert.That(refused.RejectionReason, Does.Contain("cannot hold both"));
            Assert.That(Head(target), Is.EqualTo(-1));
        }
        Assert.That(ChairHolder(colony), Is.EqualTo(admin.Id), "the refused seat leaves the chair alone");
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));

        // The order refuses the same way and changes nothing.
        SeatIndustryHeadOrder.Create(colony, admin.Id).Execute(colony.StarSysDateTime);
        Assert.That(Head(colony), Is.EqualTo(-1));
        Assert.That(ChairHolder(colony), Is.EqualTo(admin.Id));
    }

    [Test]
    public void IndustryHead_CannotTakeTheCityHallChair()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var other = MakeColony(session.FactionId, name: "Other");
        var admin = HireAdmin(colony);
        Assert.That(_server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id)).Accepted, Is.True);

        foreach (var target in new[] { colony, other })
        {
            var refused = _server.SubmitCommand(session, new AssignAdministratorCommand(target.Id, admin.Id));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason, Does.Contain("is an industry head"));
            Assert.That(refused.RejectionReason, Does.Contain("cannot hold both"));
            Assert.That(ChairHolder(target), Is.EqualTo(-1));
        }
        Assert.That(Head(colony), Is.EqualTo(admin.Id), "the refused chair leaves the industry seat alone");
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));

        // The existing order refuses the same way.
        AssignAdministratorOrder.Create(colony, admin.Id, "City Hall").Execute(colony.StarSysDateTime);
        Assert.That(ChairHolder(colony), Is.EqualTo(-1));
        Assert.That(Head(colony), Is.EqualTo(admin.Id));

        // After unseating, the same person can sit the chair.
        _server.SubmitCommand(session, new UnseatIndustryHeadCommand(colony.Id));
        Assert.That(_server.SubmitCommand(session, new AssignAdministratorCommand(colony.Id, admin.Id)).Accepted, Is.True);
        Assert.That(ChairHolder(colony), Is.EqualTo(admin.Id));
    }

    [Test]
    public void UnknownNonCivilianAndForeignPeople_AreRefused()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        int otherFaction = _game.Factions.Keys.First(id => id != session.FactionId);

        var navy = CommanderFactory.Create(_game.Systems[0], session.FactionId, new CommanderDB("Captain Test", 3, CommanderTypes.Navy));
        var foreign = CommanderFactory.Create(_game.Systems[0], otherFaction, new CommanderDB("Foreign Civ", 1, CommanderTypes.Civilian));

        var unknown = _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, 987654));
        Assert.That(unknown.Accepted, Is.False);
        Assert.That(unknown.RejectionReason, Does.Contain("not found"));

        var notCivilian = _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, navy.Id));
        Assert.That(notCivilian.Accepted, Is.False);
        Assert.That(notCivilian.RejectionReason, Does.Contain("civilian"));

        var notOurs = _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, foreign.Id));
        Assert.That(notOurs.Accepted, Is.False);
        Assert.That(notOurs.RejectionReason, Does.Contain("not found"));

        var notAColony = _server.SubmitCommand(session, new SeatIndustryHeadCommand(navy.Id, navy.Id));
        Assert.That(notAColony.Accepted, Is.False);

        Assert.That(Head(colony), Is.EqualTo(-1));
        Assert.That(navy.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
    }

    [Test]
    public void Reseating_ReplacesTheHead_AndMovesAHeadBetweenColonies()
    {
        var session = Connect();
        var a = MakeColony(session.FactionId, name: "A");
        var b = MakeColony(session.FactionId, name: "B");
        var first = HireAdmin(a);
        var second = HireAdmin(a);

        _server.SubmitCommand(session, new SeatIndustryHeadCommand(a.Id, first.Id));
        Assert.That(_server.SubmitCommand(session, new SeatIndustryHeadCommand(a.Id, first.Id)).Accepted, Is.True, "seating the sitting head again is a no-op");

        _server.SubmitCommand(session, new SeatIndustryHeadCommand(a.Id, second.Id));
        Assert.That(Head(a), Is.EqualTo(second.Id));
        Assert.That(first.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1), "the replaced head is unassigned again");

        var moved = _server.SubmitCommand(session, new SeatIndustryHeadCommand(b.Id, second.Id));
        Assert.That(moved.Accepted, Is.True, moved.RejectionReason);
        Assert.That(Head(a), Is.EqualTo(-1));
        Assert.That(Head(b), Is.EqualTo(second.Id));
        Assert.That(second.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(b.Id));
    }

    [Test]
    public void TheHead_IsShownToTheOwnerOnly()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);
        _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id));
        int other = _game.Factions.Keys.First(id => id != session.FactionId);

        var theirs = _projector.ProjectEntity(colony, other).GetView<ColonyView>();
        Assert.That(theirs?.IndustryHeadId ?? -1, Is.EqualTo(-1));
        Assert.That(theirs?.IndustryHeadName, Is.Null);
    }

    [Test]
    public void IndustryView_CarriesTheHead_WhenTheColonyHasIndustry()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        var admin = HireAdmin(colony);
        if (!colony.HasDataBlob<IndustryAbilityDB>())
            colony.SetDataBlob(new IndustryAbilityDB(new Dictionary<string, IndustryAbilityDB.ProductionLine>()));
        _server.SubmitCommand(session, new SeatIndustryHeadCommand(colony.Id, admin.Id));

        var view = _projector.ProjectEntity(colony, session.FactionId).GetView<IndustryView>()!;
        Assert.That(view.IndustryHeadId, Is.EqualTo(admin.Id));
        Assert.That(view.IndustryHeadName, Is.EqualTo(admin.GetName(session.FactionId)));
    }
}
