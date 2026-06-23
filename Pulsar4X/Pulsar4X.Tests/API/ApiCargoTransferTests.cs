using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>The cargo-transfer write surface (TransferCargoCommand).</summary>
    [TestFixture]
    public class ApiCargoTransferTests : ApiTestBase
    {
        private Mineral _mineral = null!;

        /// <summary>The test faction starts with everything locked; unlock one mineral to trade.</summary>
        private Mineral FactionMineral(PlayerSession session)
        {
            var data = _game.Factions[session.FactionId].GetDataBlob<FactionInfoDB>().Data;
            var mineral = data.LockedCargoGoods.GetMineralsList().First();
            data.Unlock(mineral.UniqueID);
            return mineral;
        }

        /// <summary>An orbiting entity with cargo storage able to hold the test mineral.</summary>
        private Entity MakeStorageEntity(PlayerSession session, string name, double positionX, double maxVolume)
        {
            var star = _game.Systems[0].GetFirstEntityWithDataBlob<StarInfoDB>();
            var storage = new CargoStorageDB(_mineral.CargoTypeID, maxVolume)
            {
                TransferRate = 100,
                TransferRangeDv_mps = 1000,
            };
            var entity = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(entity, new List<BaseDataBlob>
            {
                storage,
                new PositionDB { AbsolutePosition = new Vector3(positionX, 0, 0) },
                new MassVolumeDB { MassDry = 10000 },
                new OrbitDB(star),
                new NameDB(name, session.FactionId, name),
                new OrderableDB(),
            });
            return entity;
        }

        [Test]
        public void TransferCargo_command_escrows_and_moves_the_items()
        {
            var session = Connect();
            _mineral = FactionMineral(session);
            var station = MakeStorageEntity(session, "Station", 1000000, 10000);
            var ship = MakeStorageEntity(session, "Ship", 1000100, 5000);
            station.GetDataBlob<CargoStorageDB>().AddCargoByUnit(_mineral, 100);

            var result = _server.SubmitCommand(session, new TransferCargoCommand(
                station.Id, ship.Id, new[] { new CargoTransferItem(_mineral.ID, 40) }));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);

            // The outgoing units leave the store into escrow at order creation.
            var view = _projector.ProjectEntity(station, session.FactionId).GetView<CargoStorageView>();
            var item = view!.Stores.SelectMany(s => s.Items).Single(i => i.Id == _mineral.ID);
            Assert.That(item.Units, Is.EqualTo(60));
            Assert.That(item.UnitsInEscrow, Is.EqualTo(40));

            // The transfer itself is rate-limited over time; a day is ample for 40 units.
            _server.SetTimeControl(session,
                new TimeControlRequest(TimeControlAction.StepOnce, StepLength: TimeSpan.FromDays(1)));

            Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(_mineral, false), Is.EqualTo(40),
                "expected the items to arrive at the partner");
            Assert.That(station.GetDataBlob<CargoStorageDB>().GetUnitsStored(_mineral, true), Is.EqualTo(60));
        }

        [Test]
        public void SetOrderPause_flags_the_queued_order()
        {
            var session = Connect();
            _mineral = FactionMineral(session);
            var station = MakeStorageEntity(session, "Station", 1000000, 10000);
            var ship = MakeStorageEntity(session, "Ship", 1000100, 5000);
            station.GetDataBlob<CargoStorageDB>().AddCargoByUnit(_mineral, 100);
            _server.SubmitCommand(session, new TransferCargoCommand(
                station.Id, ship.Id, new[] { new CargoTransferItem(_mineral.ID, 40) }));

            var orders = _projector.ProjectEntity(station, session.FactionId).GetView<OrdersView>();
            Assert.That(orders, Is.Not.Null, "expected the queued transfer in the orders view");
            var order = orders!.Orders.Single();
            Assert.That(order.OrderId, Is.Not.Empty);
            Assert.That(order.PauseOnAction, Is.False);

            var result = _server.SubmitCommand(session, new SetOrderPauseCommand(station.Id, order.OrderId, Pause: true));

            Assert.That(result.Accepted, Is.True, result.RejectionReason);
            orders = _projector.ProjectEntity(station, session.FactionId).GetView<OrdersView>();
            Assert.That(orders!.Orders.Single().PauseOnAction, Is.True);
        }

        [Test]
        public void TransferCargo_rejects_items_the_source_does_not_hold()
        {
            var session = Connect();
            _mineral = FactionMineral(session);
            var station = MakeStorageEntity(session, "Station", 1000000, 10000);
            var ship = MakeStorageEntity(session, "Ship", 1000100, 5000);

            var result = _server.SubmitCommand(session, new TransferCargoCommand(
                station.Id, ship.Id, new[] { new CargoTransferItem(999999, 10) }));

            Assert.That(result.Accepted, Is.False);
        }

        [Test]
        public void TransferCargo_rejects_a_partner_without_storage()
        {
            var session = Connect();
            _mineral = FactionMineral(session);
            var station = MakeStorageEntity(session, "Station", 1000000, 10000);
            station.GetDataBlob<CargoStorageDB>().AddCargoByUnit(_mineral, 100);

            var bystander = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(bystander, new List<BaseDataBlob>
            {
                new NameDB("No Storage", session.FactionId, "No Storage"),
            });

            var result = _server.SubmitCommand(session, new TransferCargoCommand(
                station.Id, bystander.Id, new[] { new CargoTransferItem(_mineral.ID, 10) }));

            Assert.That(result.Accepted, Is.False);
        }

        [Test]
        public void TransferCargo_rejects_non_positive_amounts()
        {
            var session = Connect();
            _mineral = FactionMineral(session);
            var station = MakeStorageEntity(session, "Station", 1000000, 10000);
            var ship = MakeStorageEntity(session, "Ship", 1000100, 5000);
            station.GetDataBlob<CargoStorageDB>().AddCargoByUnit(_mineral, 100);

            var result = _server.SubmitCommand(session, new TransferCargoCommand(
                station.Id, ship.Id, new[] { new CargoTransferItem(_mineral.ID, -10) }));

            Assert.That(result.Accepted, Is.False);
        }
    }
}
