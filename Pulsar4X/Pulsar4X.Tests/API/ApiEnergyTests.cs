using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{
    /// <summary>The power-generation read surface (EnergyView) behind the power display window.</summary>
    [TestFixture]
    public class ApiEnergyTests : ApiTestBase
    {
        [Test]
        public void EnergyView_projects_stored_energy_and_the_histogram_in_ring_order()
        {
            var session = Connect();
            var data = _game.Factions[session.FactionId].GetDataBlob<Pulsar4X.Factions.FactionInfoDB>().Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();

            var generator = new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
            {
                EnergyType = energyGood,
                MaxOutputFromReactor = 500,
                EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 750 },
                EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 1000 },
                HistogramIndex = 1,
                Histogram = new List<(double outputval, double demandval, double storval, int seconds)>
                {
                    (1, 2, 3, 60),
                    (4, 5, 6, 0),
                },
            };

            var ship = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new Pulsar4X.Movement.PositionDB { AbsolutePosition = Vector3.Zero },
                new MassVolumeDB { MassDry = 1000 },
                new NameDB("Generator", session.FactionId, "Generator"),
                generator,
            });

            var view = _projector.ProjectEntity(ship, session.FactionId).GetView<EnergyView>();

            Assert.That(view, Is.Not.Null);
            Assert.That(view!.Stored, Is.EqualTo(750));
            Assert.That(view.StoreMax, Is.EqualTo(1000));
            Assert.That(view.MaxOutput, Is.EqualTo(500));
            // Unrolled from the ring buffer starting at HistogramIndex (the oldest sample).
            Assert.That(view.Histogram.Select(h => h.Seconds), Is.EqualTo(new[] { 0, 60 }).AsCollection);
        }

        private Entity AddShip(int factionId, Pulsar4X.Energy.EnergyGenAbilityDB? generator)
        {
            var ship = Entity.Create(factionId);
            var blobs = new List<BaseDataBlob>
            {
                new Pulsar4X.Movement.PositionDB { AbsolutePosition = Vector3.Zero },
                new MassVolumeDB { MassDry = 1000 },
                new NameDB("Powered", factionId, "Powered"),
                new Pulsar4X.Ships.ShipInfoDB(),
            };
            if (generator != null)
                blobs.Add(generator);
            _game.Systems[0].AddEntity(ship, blobs);
            return ship;
        }

        [Test]
        public void ShipPowerView_ShowsGenerationAgainstDemand_ToTheOwnerOnly()
        {
            var session = Connect();
            Assert.That(_game.Factions[session.FactionId].TryGetDataBlob<Pulsar4X.Factions.FactionInfoDB>(out var info), Is.True);
            var data = info!.Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();

            var generator = new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
            {
                EnergyType = energyGood,
                MaxOutputFromReactor = 300,
                MaxOutputFromSolar = 100,
                EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 750 },
                EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 1000 },
            };
            var ship = AddShip(session.FactionId, generator);
            // Same instant as the generator's last process, so this adds demand without integrating any time.
            generator.AddDemand(800, _game.TimePulse.GameGlobalDateTime);

            var view = _projector.ProjectEntity(ship, session.FactionId).GetView<ShipPowerView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.HasPlant, Is.True);
            Assert.That(view.Generation_kW, Is.EqualTo(400), "reactor plus solar");
            Assert.That(view.Demand_kW, Is.EqualTo(800));
            Assert.That(view.Stored_kJ, Is.EqualTo(750));
            Assert.That(view.StoreMax_kJ, Is.EqualTo(1000));
            Assert.That(view.Share, Is.EqualTo(0.5).Within(1e-9));

            Assert.That(_projector.ProjectEntity(ship, _game.GameMasterFaction.Id).GetView<ShipPowerView>(), Is.Null,
                "ship power must not leak to other factions");
        }

        [Test]
        public void ShipPowerView_NoPlantOrNoDraw_ShowsFull()
        {
            var session = Connect();
            Assert.That(_game.Factions[session.FactionId].TryGetDataBlob<Pulsar4X.Factions.FactionInfoDB>(out var info), Is.True);
            var data = info!.Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();

            var bare = _projector.ProjectEntity(AddShip(session.FactionId, null), session.FactionId).GetView<ShipPowerView>();
            Assert.That(bare, Is.EqualTo(new ShipPowerView(0, 0, 0, 0, false)), "an owned ship with no plant still gets a view");
            Assert.That(bare!.Share, Is.EqualTo(1), "nothing draws power, so the ring shows full");

            var idle = _projector.ProjectEntity(AddShip(session.FactionId, new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
            {
                EnergyType = energyGood,
                MaxOutputFromReactor = 50,
                EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 0 },
                EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 0 },
            }), session.FactionId).GetView<ShipPowerView>();
            Assert.That(idle!.HasPlant, Is.True);
            Assert.That(idle.Demand_kW, Is.EqualTo(0));
            Assert.That(idle.Share, Is.EqualTo(1));
        }

        [Test]
        public void EnergyView_is_owner_only()
        {
            var session = Connect();
            var data = _game.Factions[session.FactionId].GetDataBlob<Pulsar4X.Factions.FactionInfoDB>().Data;
            var energyGood = data.CargoGoods.GetAll().Values.Concat(data.LockedCargoGoods.GetAll().Values).First();

            var ship = Entity.Create(session.FactionId);
            _game.Systems[0].AddEntity(ship, new List<BaseDataBlob>
            {
                new Pulsar4X.Movement.PositionDB { AbsolutePosition = Vector3.Zero },
                new MassVolumeDB { MassDry = 1000 },
                new NameDB("Generator", session.FactionId, "Generator"),
                new Pulsar4X.Energy.EnergyGenAbilityDB(_game.TimePulse.GameGlobalDateTime)
                {
                    EnergyType = energyGood,
                    EnergyStored = new Dictionary<string, double> { [energyGood.UniqueID] = 1 },
                    EnergyStoreMax = new Dictionary<string, double> { [energyGood.UniqueID] = 1 },
                },
            });

            Assert.That(_projector.ProjectEntity(ship, _game.GameMasterFaction.Id).GetView<EnergyView>(), Is.Null,
                "power internals must not leak to other factions");
        }
    }
}
