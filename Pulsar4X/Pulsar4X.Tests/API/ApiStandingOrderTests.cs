using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Fleets;

namespace Pulsar4X.Tests
{
    /// <summary>The standing (conditional) orders surface: the whole-list replace command and the
    /// FleetSnapshot projection the client editor round-trips through.</summary>
    [TestFixture]
    public class ApiStandingOrderTests : ApiTestBase
    {
        private int MakeFleet(PlayerSession session)
        {
            var result = _server.SubmitCommand(session,
                new CreateFleetCommand(session.FactionId, _game.Systems[0].ID));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            return fleets.Last().Id;
        }

        private static StandingOrder RefuelWhenLow() => new(
            "Refuel when low",
            new[]
            {
                new StandingOrderCondition(StandingOrderTypes.FuelCondition, StandingOrderComparison.LessThan, 30f),
            },
            new[] { StandingOrderTypes.MoveToNearestColony, StandingOrderTypes.Refuel });

        [Test]
        public void SetStandingOrders_round_trips_through_the_fleet_snapshot()
        {
            var session = Connect();
            int fleetId = MakeFleet(session);
            var order = RefuelWhenLow();

            var result = _server.SubmitCommand(session, new SetStandingOrdersCommand(fleetId, new[] { order }));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            var projected = fleets.First(f => f.Id == fleetId).StandingOrders;

            Assert.That(projected, Has.Count.EqualTo(1));
            Assert.That(projected[0].Name, Is.EqualTo(order.Name));
            Assert.That(projected[0].Conditions, Is.EqualTo(order.Conditions).AsCollection);
            Assert.That(projected[0].Actions, Is.EqualTo(order.Actions).AsCollection);
        }

        [Test]
        public void SetStandingOrders_replaces_the_whole_list()
        {
            var session = Connect();
            int fleetId = MakeFleet(session);
            _server.SubmitCommand(session, new SetStandingOrdersCommand(fleetId, new[] { RefuelWhenLow() }));

            var result = _server.SubmitCommand(session,
                new SetStandingOrdersCommand(fleetId, Array.Empty<StandingOrder>()));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            var (fleets, _) = _projector.ProjectFleetHierarchy(session.FactionId);
            Assert.That(fleets.First(f => f.Id == fleetId).StandingOrders, Is.Empty);
        }

        [Test]
        public void SetStandingOrders_rejects_an_unknown_action_without_touching_the_list()
        {
            var session = Connect();
            int fleetId = MakeFleet(session);
            _server.SubmitCommand(session, new SetStandingOrdersCommand(fleetId, new[] { RefuelWhenLow() }));

            var bad = new StandingOrder("Bad", Array.Empty<StandingOrderCondition>(), new[] { "action:no-such" });
            var result = _server.SubmitCommand(session, new SetStandingOrdersCommand(fleetId, new[] { bad }));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("Unknown standing-order action"));

            // The validated-then-replace semantics kept the existing order intact.
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(fleetId, out var fleet), Is.True);
            Assert.That(fleet.GetDataBlob<FleetDB>().StandingOrders, Has.Count.EqualTo(1));
        }

        [Test]
        public void SetStandingOrders_rejects_an_unknown_condition()
        {
            var session = Connect();
            int fleetId = MakeFleet(session);

            var bad = new StandingOrder("Bad",
                new[] { new StandingOrderCondition("condition:no-such", StandingOrderComparison.LessThan, 1f) },
                Array.Empty<string>());
            var result = _server.SubmitCommand(session, new SetStandingOrdersCommand(fleetId, new[] { bad }));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("Unknown standing-order condition"));
        }

        [Test]
        public void SetStandingOrders_rejects_a_non_fleet_target()
        {
            var session = Connect();
            int bodyId = ProjectSystem(session).Entities.First().Id;
            Assert.That(_game.GlobalManager.TryGetGlobalEntityById(bodyId, out var body), Is.True);
            body.FactionOwnerID = session.FactionId;

            var result = _server.SubmitCommand(session,
                new SetStandingOrdersCommand(bodyId, new[] { RefuelWhenLow() }));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("not a fleet"));
        }
    }
}
