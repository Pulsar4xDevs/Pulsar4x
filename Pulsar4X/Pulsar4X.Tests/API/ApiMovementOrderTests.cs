using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Tests
{
    /// <summary>The ship movement-order write surface: queued newtonian burns
    /// (NewtonThrustCommand), warp moves (WarpMoveCommand) and order cancellation
    /// (CancelOrderCommand), plus the editable-maneuver order projection the maneuver
    /// panel edits from.</summary>
    [TestFixture]
    public class ApiMovementOrderTests : ApiTestBase
    {
        private Entity MakeManeuverShip(PlayerSession session)
        {
            var ship = Entity.Create(session.FactionId);
            var thrustAbility = new Pulsar4X.Movement.NewtonThrustAbilityDB("test-fuel")
            {
                ThrustInNewtons = 100000,
                ExhaustVelocity = 3000,
                FuelBurnRate = 1,
            };

            // Warp execution draws stored energy; any cargo good serves as the energy type.
            var data = _game.Factions[session.FactionId].GetDataBlob<Pulsar4X.Factions.FactionInfoDB>().Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();

            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new Pulsar4X.Movement.PositionDB { AbsolutePosition = new Vector3(1.5e11, 0, 0) },
                new MassVolumeDB { MassDry = 10000 },
                new NameDB("Maneuver Ship", session.FactionId, "Maneuver Ship"),
                new OrderableDB(),
                new Pulsar4X.Movement.WarpAbilityDB { MaxSpeed = 100000, EnergyType = energyGood.UniqueID },
                new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
                {
                    EnergyType = energyGood,
                    EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 1e9 },
                    EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 1e9 },
                },
                thrustAbility,
            });

            // ~550 m/s of ΔV available (2t of fuel pushing a 10t dry mass).
            thrustAbility.SetFuel(2000, 12000);

            // Put the ship in a real orbit so the engine's movement prediction has a state to work from.
            var star = _game.Systems[0].GetFirstEntityWithDataBlob<StarInfoDB>();
            ship.SetDataBlob(OrbitDB.FromAsteroidFormat_r(
                star, star.GetDataBlob<MassVolumeDB>().MassTotal, 12000,
                semiMajorAxis_m: 1.5e11, eccentricity: 0, inclination: 0,
                longitudeOfAscendingNode: 0, argumentOfPeriapsis: 0, meanAnomaly: 0,
                epoch: _game.Systems[0].StarSysDateTime));
            return ship;
        }

        private IReadOnlyList<OrderSnapshot> ProjectOrders(PlayerSession session, Entity ship)
            => _projector.ProjectEntity(ship, session.FactionId).GetView<OrdersView>()?.Orders
               ?? Array.Empty<OrderSnapshot>();

        [Test]
        public void NewtonThrust_queues_an_editable_maneuver()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            var nodeTime = _game.Systems[0].StarSysDateTime + TimeSpan.FromHours(2);

            var result = _server.SubmitCommand(session,
                new Pulsar4X.Api.NewtonThrustCommand(ship.Id, nodeTime, new Vec3(5, 100, 0)));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            var orders = ProjectOrders(session, ship);
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0].IsEditableManeuver, Is.True);
            Assert.That(orders[0].ManeuverNodeTime, Is.EqualTo(nodeTime),
                "the maneuver panel edits the order from the snapshot alone");
            Assert.That(orders[0].ManeuverDeltaVMps, Is.EqualTo(new Vec3(5, 100, 0)));
        }

        [Test]
        public void NewtonThrust_rejects_a_burn_beyond_available_dv()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new Pulsar4X.Api.NewtonThrustCommand(
                ship.Id, _game.Systems[0].StarSysDateTime, new Vec3(0, 1e9, 0)));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("ΔV"));
        }

        [Test]
        public void CancelOrder_removes_a_queued_maneuver()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            _server.SubmitCommand(session, new Pulsar4X.Api.NewtonThrustCommand(
                ship.Id, _game.Systems[0].StarSysDateTime + TimeSpan.FromHours(2), new Vec3(0, 100, 0)));
            string orderId = ProjectOrders(session, ship).Single().OrderId;

            var result = _server.SubmitCommand(session, new CancelOrderCommand(ship.Id, orderId));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ProjectOrders(session, ship), Is.Empty);
        }

        [Test]
        public void CancelOrder_rejects_an_unknown_order()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new CancelOrderCommand(ship.Id, "no-such-order"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("not in the queue"));
        }

        [Test]
        public void WarpMove_queues_a_warp_to_a_visible_body()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            // A faction-visible orbiting body, exactly as the warp window would pick one.
            int destinationId = ProjectSystem(session).Entities
                .First(e => e.Kind != BodyKind.Star && e.GetView<OrbitView>() != null && e.GetView<MassVolumeView>() != null)
                .Id;

            var result = _server.SubmitCommand(session,
                new Pulsar4X.Api.WarpMoveCommand(ship.Id, destinationId));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ProjectOrders(session, ship), Has.Count.EqualTo(1));
        }

        [Test]
        public void WarpMove_rejects_an_unknown_destination()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session,
                new Pulsar4X.Api.WarpMoveCommand(ship.Id, -42));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("not found"));
        }
    }
}
