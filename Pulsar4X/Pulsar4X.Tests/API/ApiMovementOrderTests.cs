using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>The ship movement-order write surface: queued newtonian burns
    /// (NewtonThrustAction), warp moves (WarpMoveAction) and order cancellation
    /// (CancelOrderCommand), plus the editable-maneuver order projection the maneuver
    /// panel edits from.</summary>
    [TestFixture]
    public class ApiMovementOrderTests : ApiTestBase
    {
        private Entity MakeManeuverShip(PlayerSession session)
        {
            var ship = Entity.Create(session.FactionId);
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();
            data.Unlock("methalox");
            var fuel = data.CargoGoods.GetAny("methalox");
            Assert.That(fuel, Is.Not.Null, "test universe has methalox after Unlock");

            var thrustAbility = new Pulsar4X.Movement.NewtonThrustAbilityDB(fuel.UniqueID)
            {
                ThrustInNewtons = 100000,
                ExhaustVelocity = 3000,
                FuelBurnRate = 1,
            };

            // Circularise / Hohmann execute on submit and NewtonSimpleProcessor reads cargo fuel.
            var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
            storage.AddCargoByUnit(fuel, 1_000_000);

            const double wetKg = 10000;
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new Pulsar4X.Movement.PositionDB { AbsolutePosition = new Vector3(1.5e11, 0, 0) },
                MassVolumeDB.NewFromMassAndRadius_m(wetKg, 10),
                new NameDB("Maneuver Ship", session.FactionId, "Maneuver Ship"),
                new ActionQueueDB(),
                new Pulsar4X.Movement.WarpAbilityDB { MaxSpeed = 100000, EnergyType = energyGood.UniqueID },
                new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
                {
                    EnergyType = energyGood,
                    EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 1e9 },
                    EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 1e9 },
                },
                thrustAbility,
                storage,
            });

            // ~20 km/s so a leftover circularise does not fail the fuel check on the first tick.
            double dryKg = wetKg / Math.Exp(20_000 / thrustAbility.ExhaustVelocity);
            thrustAbility.SetFuel(Math.Max(wetKg - dryKg, 1), wetKg);

            // Put the ship in a real orbit so the engine's movement prediction has a state to work from.
            var star = _game.Systems[0].GetFirstEntityWithDataBlob<StarInfoDB>();
            ship.SetDataBlob(OrbitDB.FromAsteroidFormat_r(
                star, star.GetDataBlob<MassVolumeDB>().MassTotal, wetKg,
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
            // The warp starts on submit (transit time is now) and queues a circularise
            // behind it so arrival burns from the real exit state.
            var orders = ProjectOrders(session, ship);
            Assert.That(orders.Select(o => o.Name), Has.Some.StartsWith("Warp Move"));
            Assert.That(orders.Select(o => o.Name), Has.Some.EqualTo("Circularise"));
            Assert.That(orders, Has.Count.EqualTo(2));
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

        int VisibleBodyId(PlayerSession session)
            => ProjectSystem(session).Entities
                .First(e => e.Kind != BodyKind.Star && e.GetView<OrbitView>() != null && e.GetView<MassVolumeView>() != null)
                .Id;

        [Test]
        public void GoToBody_emptyChair_queuesPlannerActions_withoutAGoal()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            int destinationId = VisibleBodyId(session);

            var result = _server.SubmitCommand(session, new GoToBodyCommand(ship.Id, destinationId));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ship.HasDataBlob<GoalsDB>(), Is.False);
            var names = ProjectOrders(session, ship).Select(o => o.Name).ToList();
            Assert.That(names, Has.Some.StartsWith("Warp Move").Or.EqualTo("Circularise")
                .Or.EqualTo("Change altitude").Or.StartsWith("Match orbit"));
            Assert.That(names, Is.Not.Empty);
        }

        [Test]
        public void GoToBody_rejectsASeatedCaptain()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            ship.SetDataBlob(new Pulsar4X.Ships.ShipInfoDB { CommanderID = 1 });

            var result = _server.SubmitCommand(session, new GoToBodyCommand(ship.Id, VisibleBodyId(session)));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("captain"));
            Assert.That(ProjectOrders(session, ship), Is.Empty);
        }

        [Test]
        public void WarpToBody_emptyChair_queuesWarpAndCircularise()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new WarpToBodyCommand(ship.Id, VisibleBodyId(session)));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            var names = ProjectOrders(session, ship).Select(o => o.Name).ToList();
            Assert.That(names, Has.Some.StartsWith("Warp Move"));
            Assert.That(names, Has.Some.EqualTo("Circularise"));
        }

        [Test]
        public void Circularise_emptyChair_queuesCircularise()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            var star = _game.Systems[0].GetFirstEntityWithDataBlob<StarInfoDB>();
            ship.SetDataBlob(OrbitDB.FromAsteroidFormat_r(
                star, star.GetDataBlob<MassVolumeDB>().MassTotal, 12000,
                semiMajorAxis_m: 1.5e11, eccentricity: 0.2, inclination: 0,
                longitudeOfAscendingNode: 0, argumentOfPeriapsis: 0, meanAnomaly: 0,
                epoch: _game.Systems[0].StarSysDateTime));

            var result = _server.SubmitCommand(session, new CirculariseCommand(ship.Id));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ship.HasDataBlob<GoalsDB>(), Is.False);
            Assert.That(ProjectOrders(session, ship).Select(o => o.Name), Has.Some.EqualTo("Circularise"));
        }

        [Test]
        public void Circularise_rejectsASeatedCaptain()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);
            ship.SetDataBlob(new ShipInfoDB { CommanderID = 1 });

            var result = _server.SubmitCommand(session, new CirculariseCommand(ship.Id));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("captain"));
            Assert.That(ProjectOrders(session, ship), Is.Empty);
        }

        [Test]
        public void ChangeAltitude_rejectsANonPositiveRadius()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new ChangeAltitudeCommand(ship.Id, 0));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Does.Contain("radius"));
        }

        [Test]
        public void ChangeAltitude_emptyChair_queuesChangeAltitude()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new ChangeAltitudeCommand(ship.Id, 2e11));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ship.HasDataBlob<GoalsDB>(), Is.False);
            Assert.That(ProjectOrders(session, ship).Select(o => o.Name), Has.Some.EqualTo("Change altitude"));
        }

        [Test]
        public void MatchOrbit_emptyChair_queuesMatchOrbit()
        {
            var session = Connect();
            var ship = MakeManeuverShip(session);

            var result = _server.SubmitCommand(session, new MatchOrbitCommand(ship.Id, VisibleBodyId(session)));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            Assert.That(ship.HasDataBlob<GoalsDB>(), Is.False);
            Assert.That(ProjectOrders(session, ship).Select(o => o.Name), Has.Some.StartsWith("Match orbit"));
        }
    }
}
