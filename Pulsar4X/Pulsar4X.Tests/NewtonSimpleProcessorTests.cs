using System;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// SimpleNewt longer burns: fuel is the clock, 30s pulses slice the start→target orbit.
    /// </summary>
    public class NewtonSimpleProcessorTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;
        private DateTime _epoch;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            return new Game(new NewGameSettings(), modDataStore);
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var sys in Game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _sys = new StarSystem();
            _sys.Initialize(Game, "SimpleNewt", -1);
            _sys.IncrementExternalObserver(priority: true);
            _epoch = _sys.StarSysDateTime;
        }

        [Test]
        public void InstantBurn_HighBurnRate_CompletesOnFirstPulse()
        {
            var (ship, _, targetKE, db) = MakeCirculariseBurn(fuelBurnRateKgS: 1e6, deltaVBudget: 20_000);

            NewtonSimpleProcessor.ProcessEntity(ship, _epoch + TimeSpan.FromSeconds(30));

            Assert.IsTrue(db.IsComplete, "enough fuel in one pulse should snap to the target orbit");
            Assert.IsFalse(db.IsFailed);
            Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var orbit));
            Assert.Less(orbit.Eccentricity, 0.05, "target was circularise");
            Assert.AreEqual(targetKE.SemiMajorAxis, orbit.SemiMajorAxis, targetKE.SemiMajorAxis * 0.02);
        }

        [Test]
        public void SlicedBurn_LowBurnRate_InterpolatesThenCompletes()
        {
            var (ship, startKE, targetKE, db) = MakeCirculariseBurn(fuelBurnRateKgS: 50, deltaVBudget: 20_000);

            var dv = DeltaVBetween(startKE, targetKE, _epoch);
            var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var fuelTotal = OrbitMath.TsiolkovskyFuelUse(mass, ve, dv);
            var tBurn = TimeSpan.FromSeconds(fuelTotal / 50);
            Assert.Greater(tBurn.TotalSeconds, 60, "precondition: burn does not fit in one 30s pulse");

            NewtonSimpleProcessor.ProcessEntity(ship, _epoch + TimeSpan.FromSeconds(30));

            Assert.IsFalse(db.IsComplete, "30s is only a slice of the burn");
            Assert.IsFalse(db.IsFailed);
            Assert.Less(db.CurrentTrajectory.Eccentricity, startKE.Eccentricity,
                "slice should move eccentricity toward the circular target");
            Assert.Greater(db.CurrentTrajectory.Eccentricity, targetKE.Eccentricity + 0.01,
                "slice should not already be the target orbit");

            NewtonSimpleProcessor.ProcessEntity(ship, _epoch + tBurn + TimeSpan.FromSeconds(1));

            Assert.IsTrue(db.IsComplete, "after t_burn the interpolation should reach the target");
            Assert.IsFalse(db.IsFailed);
            Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var orbit));
            Assert.Less(orbit.Eccentricity, 0.05);
        }

        [Test]
        public void ShortDeltaV_FailsWithoutSnappingToTarget()
        {
            var (ship, startKE, _, db) = MakeCirculariseBurn(fuelBurnRateKgS: 1e6, deltaVBudget: 1);

            var dv = DeltaVBetween(startKE, db.TargetTrajectory, _epoch);
            Assert.Greater(dv, 10, "precondition: manoeuvre needs more than the 1 m/s tank");

            NewtonSimpleProcessor.ProcessEntity(ship, _epoch + TimeSpan.FromSeconds(30));

            Assert.IsTrue(db.IsFailed, "short tank Δv must fail, not hang Running");
            Assert.IsFalse(db.IsComplete);
            Assert.Greater(db.CurrentTrajectory.Eccentricity, 0.1,
                "must not snap to the circular target when the burn cannot finish");
        }

        (Entity ship, KeplerElements startKE, KeplerElements targetKE, NewtonSimpleMoveDB db)
            MakeCirculariseBurn(double fuelBurnRateKgS, double deltaVBudget)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var planet = Entity.Create();
            var planetMass = 0.64174e24;
            var planetOrbit = OrbitDB.FromAsteroidFormat(sol, sol.GetDataBlob<MassVolumeDB>().MassDry,
                planetMass, 1.524, 0, 0, 0, 0, 0, _epoch);
            _sys.AddEntity(planet, new BaseDataBlob[]
            {
                new PositionDB(),
                MassVolumeDB.NewFromMassAndRadius_m(planetMass, 3_396_200),
                planetOrbit,
                new NameDB("body")
            });
            OrbitProcessor.ProcessEntity(planet, _epoch);

            const double r = 1e7;
            var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, _epoch);
            var shipAbs = planetAbs + new Vector3(r, 0, 0);

            var faction = FactionFactory.CreateFaction(Game, "newt-" + Guid.NewGuid().ToString("N"));
            var ship = Entity.Create();
            var pos = new PositionDB { AbsolutePosition = shipAbs };
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                new NameDB("burner")
            });
            pos.SetParent(planet);
            ship.FactionOwnerID = faction.Id;

            var sgp = OrbitMath.SGP(planet, ship);
            var posRel = new Vector3(r, 0, 0);
            var vCirc = Math.Sqrt(sgp / r);
            var startKE = OrbitMath.KeplerFromPositionAndVelocity(
                sgp, posRel, new Vector3(0, vCirc * 1.4, 0), _epoch);
            var targetKE = OrbitMath.KeplerCircularFromPosition(sgp, posRel, _epoch);
            Assert.Greater(startKE.Eccentricity, 0.2, "precondition: start is not circular");
            Assert.Less(targetKE.Eccentricity, 0.01, "precondition: target is circular");

            var data = faction.GetDataBlob<FactionInfoDB>().Data;
            data.Unlock("methalox");
            var fuel = data.CargoGoods.GetAny("methalox");
            Assert.IsNotNull(fuel);
            var thrust = new NewtonThrustAbilityDB(fuel.UniqueID)
            {
                ThrustInNewtons = fuelBurnRateKgS * 10_000,
                ExhaustVelocity = 10_000,
                FuelBurnRate = fuelBurnRateKgS
            };
            double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            double dry = wet / Math.Exp(deltaVBudget / thrust.ExhaustVelocity);
            thrust.SetFuel(Math.Max(wet - dry, 1), wet);
            ship.SetDataBlob(thrust);

            var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
            storage.AddCargoByUnit(fuel, 10_000_000);
            ship.SetDataBlob(storage);

            var db = new NewtonSimpleMoveDB(planet, startKE, targetKE, _epoch);
            ship.SetDataBlob(db);
            return (ship, startKE, targetKE, db);
        }

        static double DeltaVBetween(KeplerElements start, KeplerElements target, DateTime at)
        {
            var v0 = (Vector3)OrbitMath.GetStateVectors(start, at).velocity;
            var v1 = (Vector3)OrbitMath.GetStateVectors(target, at).velocity;
            return (v1 - v0).Length();
        }
    }
}
