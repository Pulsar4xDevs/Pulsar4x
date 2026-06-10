using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Messaging;

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
