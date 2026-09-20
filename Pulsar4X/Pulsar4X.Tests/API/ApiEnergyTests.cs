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
