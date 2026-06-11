using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Messaging;
using Pulsar4X.Technology;
using NameDB = Pulsar4X.Names.NameDB;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Exercises the engine side of the API layer: <see cref="EngineGameServer"/> for the push-only
    /// <see cref="IGameServer"/> contract (connect, time/command writes, the event stream) and
    /// <see cref="GameProjector"/> for the engine→DTO projection. The client-side adapter and galaxy
    /// model live in Pulsar4X.Client; these tests stay free of any UI dependency.
    /// </summary>
    [TestFixture]
    public class ApiVerticalSliceTests
    {
        private Game _game = null!;
        private IGameServer _server = null!;
        private GameProjector _projector = null!;

        [SetUp]
        public void SetUp()
        {
            _game = TestingUtilities.CreateTestUniverse(1, new DateTime(2050, 1, 1));
            // Make TimeStep run synchronously on this thread so post-step reads are deterministic.
            _game.Settings.EnforceSingleThread = true;
            _server = new EngineGameServer(_game);
            _projector = new GameProjector(_game);
        }

        private PlayerSession Connect()
        {
            var result = _server.Connect(new ConnectRequest { PlayerName = "Tester" });
            Assert.That(result.Success, Is.True, result.FailureReason);
            return result.Session;
        }

        // Projects the test universe's single system for the given session's faction.
        private SystemSnapshot ProjectSystem(PlayerSession session)
            => _projector.ProjectSystem(_game.Systems[0].ID, session.FactionId)!;

        [Test]
        public void Connect_binds_to_a_real_faction()
        {
            var session = Connect();
            Assert.That(_game.Factions.ContainsKey(session.FactionId), Is.True);
            Assert.That(session.SessionId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void SystemSnapshot_projects_visible_bodies_with_views()
        {
            var session = Connect();
            var snapshot = ProjectSystem(session);

            Assert.That(snapshot.Entities, Is.Not.Empty, "expected the system's default-visible bodies");
            Assert.That(snapshot.Entities.All(e => e.GetView<NameView>() != null), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<PositionView>()), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<OrbitView>()), Is.True);

            // Round-tripping a single entity yields the same identity.
            var first = snapshot.Entities.First();
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(first.Id, out var entity), Is.True);
            Assert.That(_projector.ProjectEntity(entity, session.FactionId).Id, Is.EqualTo(first.Id));
        }

        [Test]
        public void SystemSnapshot_projects_body_star_and_mass_views()
        {
            var session = Connect();
            var entities = ProjectSystem(session).Entities;

            // A generated system always has a star and orbiting bodies with mass.
            Assert.That(entities.Any(e => e.HasView<StarView>()), Is.True, "expected a StarView");
            Assert.That(entities.Any(e => e.HasView<BodyView>()), Is.True, "expected a BodyView");
            Assert.That(entities.Any(e => e.HasView<MassVolumeView>()), Is.True, "expected a MassVolumeView");

            var star = entities.First(e => e.HasView<StarView>()).GetView<StarView>()!;
            Assert.That(star.SurfaceTemperatureC, Is.GreaterThan(0));
            Assert.That(string.IsNullOrEmpty(star.SpectralType), Is.False);
        }

        [Test]
        public void SystemSnapshot_classifies_body_kinds()
        {
            var session = Connect();
            var entities = ProjectSystem(session).Entities;

            Assert.That(entities.Any(e => e.Kind == BodyKind.Star), Is.True, "expected a star");
            Assert.That(
                entities.Any(e => e.Kind is BodyKind.Planet or BodyKind.DwarfPlanet or BodyKind.Moon
                                       or BodyKind.Asteroid or BodyKind.Comet),
                Is.True, "expected at least one orbiting celestial body");
        }

        [Test]
        public void Subscribe_pushes_initial_state()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();

            // The initial state is pushed synchronously on Subscribe (no client fetch).
            using (_server.Subscribe(session, received.Add))
            {
                Assert.That(received.Any(e => e.Type == GameEventType.TimeChanged && e.Time != null), Is.True,
                    "expected an initial TimeChanged push");
                Assert.That(received.Any(e => e.Type == GameEventType.FactionChanged && e.Faction != null), Is.True,
                    "expected an initial FactionChanged push");
                Assert.That(received.Any(e => e.Type == GameEventType.FleetsChanged && e.Fleets != null), Is.True,
                    "expected an initial FleetsChanged push");
            }
        }

        [Test]
        public async Task FleetReorganized_message_pushes_a_FleetsChanged_delta()
        {
            // Fleet ops (create fleet, assign/transfer ship, …) reshape the tree without an entity
            // add/remove, so the engine signals them with a FleetReorganized message. Verify the server
            // turns that into a FleetsChanged push (the bug: the list didn't update on those ops).
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using (_server.Subscribe(session, received.Add))
            {
                received.Clear(); // discard the initial connect push

                await MessagePublisher.Instance.Publish(
                    Message.Create(MessageTypes.FleetReorganized, factionId: session.FactionId));

                Assert.That(received.Any(e => e.Type == GameEventType.FleetsChanged && e.Fleets != null), Is.True);
            }
        }

        [Test]
        public void GeoSurveyView_is_faction_scoped()
        {
            var session = Connect();
            // The procedurally-generated test universe doesn't attach survey blobs (only the
            // blueprint/JSON body paths do), so attach one to a visible body explicitly.
            int bodyId = ProjectSystem(session).Entities.First(e => e.HasView<BodyView>()).Id;
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.SetDataBlob(new Pulsar4X.GeoSurveys.GeoSurveyableDB { PointsRequired = 100 });

            var view = _projector.ProjectEntity(body, session.FactionId).GetView<GeoSurveyView>();
            Assert.That(view, Is.Not.Null, "expected a GeoSurveyView once the body is surveyable");
            Assert.That(view!.IsSurveyComplete, Is.False);

            // Completing the survey for this faction flips its (faction-scoped) view.
            body.GetDataBlob<Pulsar4X.GeoSurveys.GeoSurveyableDB>().GeoSurveyStatus[session.FactionId] = 0;
            Assert.That(_projector.ProjectEntity(body, session.FactionId).GetView<GeoSurveyView>()!.IsSurveyComplete,
                Is.True);
        }

        [Test]
        public void CreateFleet_command_creates_and_pushes_the_fleet()
        {
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using (_server.Subscribe(session, received.Add))
            {
                received.Clear(); // discard the initial connect push

                var result = _server.SubmitCommand(session,
                    new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
                Assert.That(result.Accepted, Is.True, result.RejectionReason);

                var (fleets, unattached) = _projector.ProjectFleetHierarchy(session.FactionId);
                Assert.That(fleets, Has.Count.EqualTo(1));
                Assert.That(fleets[0].SystemId, Is.EqualTo(_game.Systems[0].ID));
                Assert.That(unattached, Is.Empty);

                // The fleet op raised FleetReorganized, which became a self-contained FleetsChanged push.
                var push = received.LastOrDefault(e => e.Type == GameEventType.FleetsChanged);
                Assert.That(push, Is.Not.Null, "expected a FleetsChanged push");
                Assert.That(push!.Fleets, Has.Count.EqualTo(1));
                Assert.That(push.UnattachedShips, Is.Not.Null);
            }
        }

        [Test]
        public void DisbandFleet_command_removes_the_fleet()
        {
            var session = Connect();
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(fleets, Has.Count.EqualTo(1));

            var result = _server.SubmitCommand(session, new DisbandFleetCommand(fleets[0].Id));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(_projector.ProjectFleetHierarchy(session.FactionId).Fleets, Is.Empty);
        }

        [Test]
        public void ChangeFleetParent_command_nests_a_fleet()
        {
            var session = Connect();
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(fleets, Has.Count.EqualTo(2));

            var result = _server.SubmitCommand(session,
                new ChangeFleetParentCommand(fleets[1].Id, fleets[0].Id));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            var (nested, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(nested, Has.Count.EqualTo(1));
            Assert.That(nested[0].SubFleets, Has.Count.EqualTo(1));
            Assert.That(nested[0].SubFleets[0].Id, Is.EqualTo(fleets[1].Id));
        }

        [Test]
        public void ProjectResearch_returns_categories_and_unlocked_techs()
        {
            var session = Connect();
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            string techId = data.LockedTechs.Keys.First();
            data.Unlock(techId);

            var research = _projector.ProjectResearch(session.FactionId);

            Assert.That(research, Is.Not.Null);
            Assert.That(research!.Categories, Is.Not.Empty, "expected tech categories from mod data");
            var tech = research.Techs.FirstOrDefault(t => t.Id == techId);
            Assert.That(tech, Is.Not.Null, "expected the unlocked tech in the snapshot");
            Assert.That(tech!.IsResearchable, Is.True);
        }

        [Test]
        public void Research_lab_commands_drive_queue_and_funding_and_push_a_refresh()
        {
            var session = Connect();
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            string techId = data.LockedTechs.Keys.First();
            data.Unlock(techId);

            // Labs are normally spawned on component installation; fabricate one directly.
            var lab = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(lab, new List<BaseDataBlob>
            {
                new ResearcherDB(new StubLabDesign()),
                new OrderableDB(),
            });

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear(); // discard the initial connect push

            var addResult = _server.SubmitCommand(session, new AddTechToQueueCommand(lab.Id, techId));
            Assert.That(addResult.Accepted, Is.True, addResult.RejectionReason);

            var view = _projector.ProjectEntity(lab, session.FactionId).GetView<ResearcherView>();
            Assert.That(view, Is.Not.Null, "expected a ResearcherView for the owning faction");
            Assert.That(view!.TechQueue, Does.Contain(techId));

            var fundingResult = _server.SubmitCommand(session, new SetResearchFundingCommand(lab.Id, 3));
            Assert.That(fundingResult.Accepted, Is.True, fundingResult.RejectionReason);
            view = _projector.ProjectEntity(lab, session.FactionId).GetView<ResearcherView>();
            Assert.That(view!.FundingLevel, Is.EqualTo(3));

            // Instant orders don't raise engine messages; the server itself pushes a refresh of the
            // commanded entity after each accepted command.
            Assert.That(received.Any(e => e.Type == GameEventType.EntityChanged
                                          && e.EntityId == lab.Id
                                          && e.Entity?.GetView<ResearcherView>() != null),
                Is.True, "expected a post-command EntityChanged push carrying the lab's new state");
        }

        [Test]
        public void Research_completion_pushes_the_advanced_lab_queue()
        {
            var session = Connect();
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            string techId = data.LockedTechs.Keys.First();
            data.Unlock(techId);

            var lab = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(lab, new List<BaseDataBlob>
            {
                // Enough output to finish any tech in a single daily research pulse, at no cost.
                new ResearcherDB(new StubLabDesign()) { PointsPerDay = new(int.MaxValue / 2), CostPerDay = new(0) },
                new OrderableDB(),
            });

            var queueResult = _server.SubmitCommand(session, new AddTechToQueueCommand(lab.Id, techId));
            Assert.That(queueResult.Accepted, Is.True, queueResult.RejectionReason);

            var received = new List<GameEventEnvelope>();
            using var subscription = _server.Subscribe(session, received.Add);
            received.Clear(); // discard the initial connect push

            // Step past the daily research pulse so the tech completes and is dequeued mid-tick.
            _server.SetTimeControl(session,
                new TimeControlRequest(TimeControlAction.StepOnce, StepLength: TimeSpan.FromDays(2)));

            Assert.That(lab.GetDataBlob<ResearcherDB>().TechQueue, Does.Not.Contain(techId),
                "expected the engine to dequeue the completed tech");

            // The completion is dequeued mid-tick with no engine message, so the server must bridge
            // the ResearchCompleted event into an EntityChanged push carrying the advanced queue.
            var push = received.LastOrDefault(e => e.Type == GameEventType.EntityChanged && e.EntityId == lab.Id);
            Assert.That(push, Is.Not.Null, "expected an EntityChanged push for the lab on research completion");
            Assert.That(push!.Entity!.GetView<ResearcherView>()!.TechQueue, Does.Not.Contain(techId));
        }

        [Test]
        public void Colony_views_project_for_the_owning_faction_only()
        {
            var session = Connect();
            var faction = _game.Factions[session.FactionId];

            // Plant a colony on a body in the test system (the default-humans start can't be used:
            // DefaultStartFactory's design-file paths predate the basemod reorganisation).
            var species = Pulsar4X.People.SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
            var colony = Pulsar4X.Colonies.ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);

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

        /// <summary>Minimal design so a ResearcherDB can exist outside the component pipeline.</summary>
        private sealed class StubLabDesign : IConstructableDesign
        {
            public ConstructableGuiHints GuiHints => ConstructableGuiHints.None;
            public string UniqueID => "stub-lab-design";
            public string Name => "Stub Lab";
            public bool IsValid => true;
            public Dictionary<string, long> ResourceCosts { get; } = new();
            public long IndustryPointCosts => 0;
            public string IndustryTypeID => "";
            public ushort OutputAmount => 1;
            public void OnConstructionComplete(Entity industryEntity, Pulsar4X.Storage.CargoStorageDB storage,
                string productionLine, Pulsar4X.Industry.IndustryJob batchJob, IConstructableDesign designInfo)
            { }
        }

        [Test]
        public void StepOnce_advances_the_clock()
        {
            var session = Connect();
            var before = _game.TimePulse.GameGlobalDateTime;
            var step = TimeSpan.FromDays(1);

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.StepOnce, StepLength: step));

            Assert.That(_game.TimePulse.GameGlobalDateTime, Is.EqualTo(before + step));
        }

        [Test]
        public void SetSpeed_changes_the_clock_multiplier()
        {
            var session = Connect();

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetSpeed, Multiplier: 4f));

            Assert.That(_game.TimePulse.TimeMultiplier, Is.EqualTo(4f));
        }

        [Test]
        public void SubmitCommand_rejects_an_unknown_entity()
        {
            var session = Connect();

            var result = _server.SubmitCommand(session, new RenameCommand(TargetEntityId: -123456, NewName: "ghost"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.Not.Null.And.Contains("not found"));
        }

        [Test]
        public void SubmitCommand_rejects_an_unowned_entity()
        {
            var session = Connect();
            // Default-visible bodies are neutral; the faction does not own them.
            int neutralBodyId = ProjectSystem(session).Entities.First().Id;

            var result = _server.SubmitCommand(session, new RenameCommand(neutralBodyId, "Mine Now"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("does not control"));
        }

        [Test]
        public void SubmitCommand_renames_an_owned_entity_and_the_effect_is_visible()
        {
            var session = Connect();
            int bodyId = ProjectSystem(session).Entities.First().Id;

            // Give the connected faction ownership so it may command the entity.
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.FactionOwnerID = session.FactionId;

            var result = _server.SubmitCommand(session, new RenameCommand(bodyId, "Faction Renamed"));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            // The engine applied the faction-scoped rename and the projection reflects it.
            Assert.That(_projector.ProjectEntity(body, session.FactionId).GetView<NameView>()!.Name,
                Is.EqualTo("Faction Renamed"));
        }
    }
}
