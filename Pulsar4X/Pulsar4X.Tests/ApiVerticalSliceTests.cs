using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// End-to-end coverage of the API layer's first vertical slice: an <see cref="InProcessAdapter"/>
    /// talking to an <see cref="EngineGameServer"/> over the <see cref="IGameClient"/>/<see cref="IGameServer"/>
    /// contracts — connect, read a system snapshot, step the clock, and route a command. No UI involved.
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

        private async Task<InProcessAdapter> ConnectedClient()
        {
            var client = new InProcessAdapter(_server);
            var result = await client.ConnectAsync(new ConnectRequest { PlayerName = "Tester" });
            Assert.That(result.Success, Is.True, result.FailureReason);
            return client;
        }

        [Test]
        public async Task Connect_binds_to_a_real_faction()
        {
            var client = await ConnectedClient();

            Assert.That(client.IsConnected, Is.True);
            Assert.That(_game.Factions.ContainsKey(client.Session.FactionId), Is.True);
            Assert.That(client.Session.SessionId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public async Task SystemSnapshot_projects_visible_bodies_with_views()
        {
            var client = await ConnectedClient();
            string systemId = _game.Systems[0].ID;

            await client.LoadSystemAsync(systemId);

            var system = client.World.GetSystem(systemId);
            Assert.That(system, Is.Not.Null);
            Assert.That(system!.Entities, Is.Not.Empty, "expected the system's default-visible bodies");

            // Every projected body carries a name; bodies have positions; orbiting bodies have orbits.
            Assert.That(system.Entities.All(e => e.GetView<NameView>() != null), Is.True);
            Assert.That(system.Entities.Any(e => e.HasView<PositionView>()), Is.True);
            Assert.That(system.Entities.Any(e => e.HasView<OrbitView>()), Is.True);

            // Round-tripping a single entity yields the same identity.
            var first = system.Entities.First();
            var single = client.World.GetSystem(systemId)!.GetEntity(first.Id);
            Assert.That(single!.Id, Is.EqualTo(first.Id));
        }

        [Test]
        public async Task StepOnce_advances_the_world_clock()
        {
            var client = await ConnectedClient();
            var before = client.World.Time.GameDateTime;
            var step = TimeSpan.FromDays(1);

            await client.SetTimeControlAsync(new TimeControlRequest(TimeControlAction.StepOnce, StepLength: step));

            Assert.That(client.World.Time.GameDateTime, Is.EqualTo(before + step));
        }

        [Test]
        public async Task SetSpeed_is_reflected_in_world_time()
        {
            var client = await ConnectedClient();

            await client.SetTimeControlAsync(new TimeControlRequest(TimeControlAction.SetSpeed, Multiplier: 4f));

            Assert.That(client.World.Time.Multiplier, Is.EqualTo(4f));
        }

        [Test]
        public async Task SubmitCommand_rejects_an_unknown_entity()
        {
            var client = await ConnectedClient();

            var result = await client.SubmitCommandAsync(new RenameCommand(TargetEntityId: -123456, NewName: "ghost"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.Not.Null.And.Contains("not found"));
        }

        [Test]
        public async Task SubmitCommand_rejects_an_unowned_entity()
        {
            var client = await ConnectedClient();
            string systemId = _game.Systems[0].ID;
            await client.LoadSystemAsync(systemId);
            // Default-visible bodies are neutral; the faction does not own them.
            int neutralBodyId = client.World.GetSystem(systemId)!.Entities.First().Id;

            var result = await client.SubmitCommandAsync(new RenameCommand(neutralBodyId, "Mine Now"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("does not control"));
        }

        [Test]
        public async Task SubmitCommand_renames_an_owned_entity_and_the_effect_is_visible()
        {
            var client = await ConnectedClient();
            string systemId = _game.Systems[0].ID;
            await client.LoadSystemAsync(systemId);
            int bodyId = client.World.GetSystem(systemId)!.Entities.First().Id;

            // Give the connected faction ownership so it may command the entity.
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.FactionOwnerID = client.Session.FactionId;

            var result = await client.SubmitCommandAsync(new RenameCommand(bodyId, "Faction Renamed"));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            // The engine applied the faction-scoped rename and the projection reflects it.
            var snapshot = _server.GetEntitySnapshot(client.Session, bodyId);
            Assert.That(snapshot!.GetView<NameView>()!.Name, Is.EqualTo("Faction Renamed"));
        }
    }
}
