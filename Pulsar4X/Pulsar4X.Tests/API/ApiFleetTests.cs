using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Messaging;
using Pulsar4X.Names;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    /// <summary>The fleet command surface and the fleet-hierarchy push.</summary>
    [TestFixture]
    public class ApiFleetTests : ApiTestBase
    {
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
                Assert.That(fleets[0].CommandSpan, Is.EqualTo(CommandSpanLabels.Body));
                Assert.That(fleets[0].LogisticsSpan, Is.EqualTo(CommandSpanLabels.Well));
                Assert.That(unattached, Is.Empty);

                // The fleet op raised FleetReorganized, which became a self-contained FleetsChanged push.
                var push = received.LastOrDefault(e => e.Type == GameEventType.FleetsChanged);
                Assert.That(push, Is.Not.Null, "expected a FleetsChanged push");
                Assert.That(push!.Fleets, Has.Count.EqualTo(1));
                Assert.That(push.UnattachedShips, Is.Not.Null);
            }
        }

        [Test]
        public async Task OrdersChanged_message_pushes_a_FleetsChanged_delta()
        {
            // Orders are carried on the fleet snapshot, so a queue mutation (queue/cancel/pause/
            // standing-order swap) signals OrdersChanged — which, like FleetReorganized, re-pushes
            // the whole fleet tree. Without this the order list froze while paused (the bug).
            var session = Connect();
            var received = new List<GameEventEnvelope>();
            using (_server.Subscribe(session, received.Add))
            {
                received.Clear(); // discard the initial connect push

                await MessagePublisher.Instance.Publish(
                    Message.Create(MessageTypes.OrdersChanged, factionId: session.FactionId));

                Assert.That(received.Any(e => e.Type == GameEventType.FleetsChanged && e.Fleets != null), Is.True);
            }
        }

        [Test]
        public void Queue_mutating_command_pushes_the_fleet_tree_while_paused()
        {
            // End to end while paused: a command that changes a fleet's order state must re-push the
            // fleet tree even though no clock advance occurs. SetStandingOrders is such a command — it
            // mutates the fleet's standing-order list directly and publishes OrdersChanged.
            var session = Connect();
            _server.SetTimeControl(session, new TimeControlRequest(TimeControlAction.Pause));
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(fleets, Has.Count.EqualTo(1));

            var received = new List<GameEventEnvelope>();
            using (_server.Subscribe(session, received.Add))
            {
                received.Clear(); // discard the initial connect push

                var result = _server.SubmitCommand(session,
                    new SetStandingOrdersCommand(fleets[0].Id, new List<StandingOrder>()));
                Assert.That(result.Accepted, Is.True, result.RejectionReason);

                var push = received.LastOrDefault(e => e.Type == GameEventType.FleetsChanged);
                Assert.That(push, Is.Not.Null, "expected a FleetsChanged push from the order-queue change");
                Assert.That(push!.Fleets, Has.Count.EqualTo(1));
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
        public void Reassign_and_detach_move_a_ship_between_fleets_and_the_unassigned_list()
        {
            var session = Connect();
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            _server.SubmitCommand(session, new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            var (fleets, looseAtStart) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(fleets, Has.Count.EqualTo(2));
            Assert.That(looseAtStart, Is.Empty);

            var ship = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new ShipInfoDB(),
                new NameDB("Courier"),
            });

            var assign = _server.SubmitCommand(session, new ReassignShipCommand(ship.Id, fleets[0].Id));
            Assert.That(assign.Accepted, Is.True, assign.RejectionReason);

            var (afterAssign, looseAfterAssign) = _projector.ProjectFleetHierarchy(session.FactionId);
            var home = afterAssign.Single(f => f.Id == fleets[0].Id);
            var other = afterAssign.Single(f => f.Id == fleets[1].Id);
            Assert.That(home.Ships.Select(s => s.Id), Is.EqualTo(new[] { ship.Id }));
            Assert.That(home.Ships[0].Name, Is.EqualTo("Courier"));
            Assert.That(home.FlagshipId, Is.EqualTo(ship.Id));
            Assert.That(other.Ships, Is.Empty);
            Assert.That(looseAfterAssign, Is.Empty);

            var received = new List<GameEventEnvelope>();
            using (_server.Subscribe(session, received.Add))
            {
                received.Clear();
                var detach = _server.SubmitCommand(session, new DetachShipCommand(ship.Id));
                Assert.That(detach.Accepted, Is.True, detach.RejectionReason);

                var push = received.LastOrDefault(e => e.Type == GameEventType.FleetsChanged);
                Assert.That(push, Is.Not.Null, "expected a FleetsChanged push");
                Assert.That(push!.UnattachedShips.Select(s => s.Id), Does.Contain(ship.Id));
            }

            var (afterDetach, loose) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(afterDetach.Single(f => f.Id == fleets[0].Id).Ships, Is.Empty);
            Assert.That(afterDetach.Single(f => f.Id == fleets[0].Id).FlagshipId, Is.Null);
            Assert.That(loose.Select(s => s.Id), Is.EqualTo(new[] { ship.Id }));
            Assert.That(loose[0].Name, Is.EqualTo("Courier"));

            var move = _server.SubmitCommand(session, new ReassignShipCommand(ship.Id, fleets[1].Id));
            Assert.That(move.Accepted, Is.True, move.RejectionReason);

            var (afterMove, looseAfterMove) = _projector.ProjectFleetHierarchy(session.FactionId);
            var destination = afterMove.Single(f => f.Id == fleets[1].Id);
            Assert.That(destination.Ships.Select(s => s.Id), Is.EqualTo(new[] { ship.Id }));
            Assert.That(destination.FlagshipId, Is.EqualTo(ship.Id));
            Assert.That(afterMove.Single(f => f.Id == fleets[0].Id).Ships, Is.Empty);
            Assert.That(looseAfterMove, Is.Empty);
        }
    }
}
