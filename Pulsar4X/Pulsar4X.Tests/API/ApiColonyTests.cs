using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.People;
using NameDB = Pulsar4X.Names.NameDB;

namespace Pulsar4X.Tests
{
    /// <summary>The colony read surface and its owner-only visibility scoping.</summary>
    [TestFixture]
    public class ApiColonyTests : ApiTestBase
    {
        [Test]
        public void Colony_views_project_for_the_owning_faction_only()
        {
            var session = Connect();
            var faction = _game.Factions[session.FactionId];

            // Plant a colony on a body in the test system (the default-humans start can't be used:
            // DefaultStartFactory's design-file paths predate the basemod reorganisation).
            var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
            var colony = ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);

            var snapshot = _projector.ProjectEntity(colony, session.FactionId);
            Assert.That(snapshot.Kind, Is.EqualTo(BodyKind.Colony));

            var colonyView = snapshot.GetView<ColonyView>();
            Assert.That(colonyView, Is.Not.Null);
            Assert.That(colonyView!.Population, Is.EqualTo(9_000_000));
            Assert.That(colonyView.SpeciesPopulations, Has.Count.EqualTo(1), "expected a species population breakdown");
            Assert.That(colonyView.PlanetEntityId, Is.EqualTo(planet.Id));

            // The owner sees the colony's internals…
            Assert.That(snapshot.GetView<InstallationsView>(), Is.Not.Null);
            Assert.That(snapshot.GetView<CargoStorageView>(), Is.Not.Null);
            Assert.That(snapshot.GetView<InfrastructureView>(), Is.Not.Null);
            Assert.That(snapshot.GetView<ColonyMiningView>(), Is.Not.Null);

            // …and other factions never do.
            var rival = _game.Factions.Values.First(f => f.Id != session.FactionId);
            var rivalSnapshot = _projector.ProjectEntity(colony, rival.Id);
            Assert.That(rivalSnapshot.HasView<CargoStorageView>(), Is.False);
            Assert.That(rivalSnapshot.HasView<InstallationsView>(), Is.False);
            Assert.That(rivalSnapshot.HasView<InfrastructureView>(), Is.False);
            Assert.That(rivalSnapshot.HasView<ColonyMiningView>(), Is.False);

            // Component command wiring: an unknown design is rejected with a real reason.
            var result = _server.SubmitCommand(session, new UninstallComponentCommand(colony.Id, "no-such-design"));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("design"));
        }

        [Test]
        public void Industry_and_construction_views_project_for_the_owner_only()
        {
            var session = Connect();
            var colony = MakeColony(session);

            // Industry/construction capability normally comes from installed components; fabricate it.
            colony.SetDataBlob(new IndustryAbilityDB("test-factory", new IndustryAbilityDB.ProductionLine
            {
                Name = "Test Factory",
                IndustryTypeRates = { ["test-industry-type"] = 10 },
            }));
            colony.SetDataBlob(new LocalConstructionDB { PointsPerDay = 100 });

            var snapshot = _projector.ProjectEntity(colony, session.FactionId);

            var industry = snapshot.GetView<IndustryView>();
            Assert.That(industry, Is.Not.Null, "expected an IndustryView for the owner");
            Assert.That(industry!.ProductionLines, Has.Count.EqualTo(1));
            Assert.That(industry.ProductionLines[0].Name, Is.EqualTo("Test Factory"));
            Assert.That(industry.ProductionLines[0].Jobs, Is.Empty);

            var construction = snapshot.GetView<ConstructionView>();
            Assert.That(construction, Is.Not.Null, "expected a ConstructionView for the owner");
            Assert.That(construction!.PointsPerDay, Is.EqualTo(100));
            Assert.That(construction.BuildQueue, Is.Empty);

            var rival = _game.Factions.Values.First(f => f.Id != session.FactionId);
            var rivalSnapshot = _projector.ProjectEntity(colony, rival.Id);
            Assert.That(rivalSnapshot.HasView<IndustryView>(), Is.False);
            Assert.That(rivalSnapshot.HasView<ConstructionView>(), Is.False);
        }

        [Test]
        public void Industry_and_construction_commands_reject_bad_targets()
        {
            var session = Connect();
            var colony = MakeColony(session);
            colony.SetDataBlob(new LocalConstructionDB { PointsPerDay = 100 });

            // Unknown design ids are rejected with a real reason…
            var queueResult = _server.SubmitCommand(session,
                new QueueIndustryJobCommand(colony.Id, "line", "no-such-design", 1, false, false));
            Assert.That(queueResult.Accepted, Is.False);
            Assert.That(queueResult.RejectionReason, Does.Contain("not found"));

            var addResult = _server.SubmitCommand(session,
                new AddToConstructionQueueCommand(colony.Id, "no-such-design"));
            Assert.That(addResult.Accepted, Is.False);
            Assert.That(addResult.RejectionReason, Does.Contain("not found"));

            // …and queue positions that don't exist are too.
            var moveResult = _server.SubmitCommand(session,
                new MoveConstructionJobCommand(colony.Id, 0, MoveUp: true));
            Assert.That(moveResult.Accepted, Is.False);
            Assert.That(moveResult.RejectionReason, Does.Contain("queue position"));
        }

        /// <summary>Plants a bare colony for the session's faction on a body in the test system.</summary>
        private Entity MakeColony(PlayerSession session)
        {
            var faction = _game.Factions[session.FactionId];
            var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
            return ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);
        }
    }
}
