using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Exercises the engine side of the API layer — <see cref="EngineGameServer"/> through the
    /// <see cref="IGameServer"/> contract: connect, faction-scoped snapshot projection, time control,
    /// and command routing/validation. The client-side adapter and galaxy model live in
    /// Pulsar4X.Client and are covered there; these tests deliberately stay free of any UI dependency.
    /// </summary>
    [TestFixture]
    public class ApiVerticalSliceTests
    {
        private Game _game = null!;
        private IGameServer _server = null!;

        [SetUp]
        public void SetUp()
        {
            _game = TestingUtilities.CreateTestUniverse(1, new DateTime(2050, 1, 1));
            // Make TimeStep run synchronously on this thread so post-step reads are deterministic.
            _game.Settings.EnforceSingleThread = true;
            _server = new EngineGameServer(_game);
        }

        private PlayerSession Connect()
        {
            var result = _server.Connect(new ConnectRequest { PlayerName = "Tester" });
            Assert.That(result.Success, Is.True, result.FailureReason);
            return result.Session;
        }

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
            var snapshot = _server.GetSystemSnapshot(session, _game.Systems[0].ID);

            Assert.That(snapshot.Entities, Is.Not.Empty, "expected the system's default-visible bodies");
            Assert.That(snapshot.Entities.All(e => e.GetView<NameView>() != null), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<PositionView>()), Is.True);
            Assert.That(snapshot.Entities.Any(e => e.HasView<OrbitView>()), Is.True);

            // Round-tripping a single entity yields the same identity.
            var first = snapshot.Entities.First();
            var single = _server.GetEntitySnapshot(session, first.Id);
            Assert.That(single!.Id, Is.EqualTo(first.Id));
        }

        [Test]
        public void SystemSnapshot_projects_body_star_and_mass_views()
        {
            var session = Connect();
            var entities = _server.GetSystemSnapshot(session, _game.Systems[0].ID).Entities;

            // A generated system always has a star and orbiting bodies with mass.
            Assert.That(entities.Any(e => e.HasView<StarView>()), Is.True, "expected a StarView");
            Assert.That(entities.Any(e => e.HasView<BodyView>()), Is.True, "expected a BodyView");
            Assert.That(entities.Any(e => e.HasView<MassVolumeView>()), Is.True, "expected a MassVolumeView");

            var star = entities.First(e => e.HasView<StarView>()).GetView<StarView>()!;
            Assert.That(star.SurfaceTemperatureC, Is.GreaterThan(0));
            Assert.That(string.IsNullOrEmpty(star.SpectralType), Is.False);
        }

        [Test]
        public void StepOnce_advances_the_clock()
        {
            var session = Connect();
            var before = _server.GetTimeState(session).GameDateTime;
            var step = TimeSpan.FromDays(1);

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.StepOnce, StepLength: step));

            Assert.That(_server.GetTimeState(session).GameDateTime, Is.EqualTo(before + step));
        }

        [Test]
        public void SetSpeed_is_reflected_in_time_state()
        {
            var session = Connect();

            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.SetSpeed, Multiplier: 4f));

            Assert.That(_server.GetTimeState(session).Multiplier, Is.EqualTo(4f));
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
            int neutralBodyId = _server.GetSystemSnapshot(session, _game.Systems[0].ID).Entities.First().Id;

            var result = _server.SubmitCommand(session, new RenameCommand(neutralBodyId, "Mine Now"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("does not control"));
        }

        [Test]
        public void SubmitCommand_renames_an_owned_entity_and_the_effect_is_visible()
        {
            var session = Connect();
            int bodyId = _server.GetSystemSnapshot(session, _game.Systems[0].ID).Entities.First().Id;

            // Give the connected faction ownership so it may command the entity.
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.FactionOwnerID = session.FactionId;

            var result = _server.SubmitCommand(session, new RenameCommand(bodyId, "Faction Renamed"));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            // The engine applied the faction-scoped rename and the projection reflects it.
            var snapshot = _server.GetEntitySnapshot(session, bodyId);
            Assert.That(snapshot!.GetView<NameView>()!.Name, Is.EqualTo("Faction Renamed"));
        }
    }
}
