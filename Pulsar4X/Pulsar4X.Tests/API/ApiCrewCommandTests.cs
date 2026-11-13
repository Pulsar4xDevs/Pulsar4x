using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>Crew plan step B: the crew command shapes reach the translator and refuse cleanly.</summary>
[TestFixture]
public class ApiCrewCommandTests : ApiTestBase
{
    Entity MakeColony(int factionId, long pop)
    {
        var faction = _game.Factions[factionId];
        var system = _game.Systems[0];
        var planet = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().First();
        var species = Entity.Create(factionId);
        system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Human") });
        return ColonyFactory.CreateColony(faction, species, planet, pop);
    }

    [Test]
    public void CrewTransfers_NeedAShipAndAnOwnColony()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId, 1_000_000);

        var notAShip = _server.SubmitCommand(session, new TopUpCrewCommand(colony.Id, colony.Id));
        Assert.That(notAShip.Accepted, Is.False);
        Assert.That(notAShip.RejectionReason, Is.EqualTo("Crew move to or from one ship."));

        var down = _server.SubmitCommand(session, new CrewToPoolCommand(colony.Id, colony.Id, 5));
        Assert.That(down.Accepted, Is.False);
        Assert.That(down.RejectionReason, Is.EqualTo("Crew move to or from one ship."));
    }

    [Test]
    public void TrainCrew_NeedsAColonyAndATrainee()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId, 1_000_000);
        var none = _server.SubmitCommand(session, new TrainAcademyCrewCommand(colony.Id, 0, 0));
        Assert.That(none.Accepted, Is.False);
        Assert.That(none.RejectionReason, Is.EqualTo("Enrol at least one trainee."));
    }
}
