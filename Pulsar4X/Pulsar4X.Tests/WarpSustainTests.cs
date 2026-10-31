using System;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Uphill warp is booked as bubble sustain. One speed, chosen when the bubble opens.
    /// </summary>
    public class WarpSustainTests
    {
        const double DriveMps = 50_000;
        const double PlantKw = 15;
        const double SustainKw = 15;
        const double Coupling = 0.0025;
        const double PureRadialMps = 25287;
        const double ShipMassKg = 40_000;

        static Game _game = InitializeGame();
        static StarSystem _starSys;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            game.Settings.EnforceSingleThread = true;
            // Arrival uses SetOrbitHereNoNewt, so the save test does not need thrust.
            game.Settings.StrictNewtonion = false;
            return game;
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var sys in _game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _starSys = new StarSystem();
            _starSys.Initialize(_game, "Sol", -1);
            _starSys.IncrementExternalObserver(priority: true);
        }

        [Test]
        public void PureRadial_1Au_PlantHoldsAbout25287()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 2, ShipMassKg, DriveMps, PlantKw, Coupling, bleed: 0);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);

            Assert.That(cruise.speed, Is.EqualTo(PureRadialMps).Within(PureRadialMps * 0.01));
            Assert.That(cruise.demandKW, Is.EqualTo(SustainKw).Within(1e-6));
            Assert.AreEqual(PlantHoldsLine(DriveMps, cruise.speed), cruise.limitLine);
        }

        [Test]
        public void PureRadial_AtMarsOrbit_DriveWins()
        {
            var (ship, target) = RadialLeg(shipAu: 1.524, targetAu: 3, ShipMassKg, DriveMps, PlantKw, Coupling, bleed: 0);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);

            Assert.AreEqual(DriveMps, cruise.speed);
            Assert.That(cruise.limitLine, Does.Contain("plant can hold it"));
            Assert.That(cruise.demandKW, Is.EqualTo(SustainKw).Within(1e-6));
        }

        [Test]
        public void PureRadial_HalfOutput_HalvesCruise()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 2, ShipMassKg, DriveMps, outputKw: 7.5, Coupling, bleed: 0);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);
            double half = PureRadialMps / 2;

            Assert.That(cruise.speed, Is.EqualTo(half).Within(half * 0.01));
            Assert.Less(cruise.speed, DriveMps);
        }

        [Test]
        public void PureRadial_DoubleMass_HalvesCruise()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 2, mass: 80_000, drive: 25_000, PlantKw, Coupling, bleed: 0);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);
            double half = PureRadialMps / 2;

            Assert.That(cruise.speed, Is.EqualTo(half).Within(half * 0.01));
        }

        [Test]
        public void Inward_BleedHalf_CapsAt25()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 0.2, ShipMassKg, DriveMps, PlantKw, Coupling, bleed: 0.5);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);

            Assert.AreEqual(25_000, cruise.speed);
            Assert.AreEqual("Downhill, bleed limited to 25 Km/s.", cruise.limitLine);
            Assert.That(cruise.demandKW, Is.EqualTo(SustainKw).Within(1e-6));
        }

        [Test]
        public void Inward_BleedZero_FullDriveEmptyLine()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 0.2, ShipMassKg, DriveMps, PlantKw, Coupling, bleed: 0);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);

            Assert.AreEqual(DriveMps, cruise.speed);
            Assert.AreEqual("", cruise.limitLine);
            Assert.That(cruise.demandKW, Is.EqualTo(SustainKw).Within(1e-6));
        }

        [Test]
        public void CouplingZero_Outward_FullDriveEmptyLine()
        {
            var (ship, target) = RadialLeg(shipAu: 1, targetAu: 2, ShipMassKg, DriveMps, PlantKw, coupling: 0, bleed: 0.5);
            var cruise = WarpMath.PriceCruise(ship, target, _starSys.StarSysDateTime, Vector3.Zero);

            Assert.AreEqual(DriveMps, cruise.speed);
            Assert.That(cruise.demandKW, Is.EqualTo(SustainKw).Within(1e-6));
            Assert.AreEqual("", cruise.limitLine);
        }

        [Test]
        public void TwoDrives_TakeWorseCouplingAndTighterBleed()
        {
            var ship = AddBody(
                Vector3.Zero,
                new ComponentInstancesDB(),
                MassVolumeDB.NewFromMassAndRadius_m(ShipMassKg, 20));

            var wide = new WarpDriveAtb(2e6, "electricity", 10, 15, 1, 0.0025, 0.5);
            var tight = new WarpDriveAtb(2e6, "electricity", 3, 4, 2, 0.001, 0.8);
            wide.OnComponentInstallation(ship, null);
            tight.OnComponentInstallation(ship, null);

            var warp = ship.GetDataBlob<WarpAbilityDB>();
            Assert.That(warp.Coupling, Is.EqualTo(0.0025).Within(1e-12));
            Assert.That(warp.BleedFraction, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(warp.BubbleCreationCost, Is.EqualTo(13).Within(1e-9));
            Assert.That(warp.BubbleSustainCost, Is.EqualTo(19).Within(1e-9));
            Assert.That(warp.BubbleCollapseCost, Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void EarthMars_SameSide_ClimbIsSlowerAndDoesNotStackDemand()
        {
            var leg = OpenEarthMars(coupling: Coupling);
            var move = leg.Ship.GetDataBlob<WarpMovingDB>();
            var power = leg.Ship.GetDataBlob<EnergyGenAbilityDB>();
            double speed = move.CurrentNonNewtonionVectorMS.Length();

            Assert.Less(speed, DriveMps);
            Assert.That(move.LimitLine, Does.Contain("uphill").IgnoreCase);
            Assert.That(leg.Action.Details, Does.Contain("uphill").IgnoreCase);
            double paid = (move.PredictedExitTime - move.EntryDateTime).TotalSeconds;
            Assert.Greater(paid, leg.FreeSeconds);
            Assert.Less(paid, leg.FreeSeconds * 3);
            Assert.That(power.Demand, Is.EqualTo(SustainKw).Within(1e-6));

            double stored = power.EnergyStored["electricity"];
            var hour = move.EntryDateTime.AddHours(1);
            WarpMoveProcessor.ProcessToDate(leg.Ship, hour);
            EnergyGenProcessor.EnergyGen(leg.Ship, hour);

            Assert.IsFalse(move.IsAtTarget);
            Assert.That(move.CurrentNonNewtonionVectorMS.Length(), Is.EqualTo(speed).Within(1e-3));
            Assert.That(power.Demand, Is.EqualTo(SustainKw).Within(1e-6));
            // A stacked overhead plus climb is 15 kW for the hour, 54000 kJ.
            Assert.That(stored - power.EnergyStored["electricity"], Is.LessThan(1000));
        }

        [Test]
        public void EarthMars_SaveLoad_RemovesSustainOnce()
        {
            int cargoIds = Game.EntityIDCounter;
            int nextId = EntityIDGenerator.NextId;
            try
            {
                var leg = OpenEarthMars(coupling: Coupling);
                var move = leg.Ship.GetDataBlob<WarpMovingDB>();
                var power = leg.Ship.GetDataBlob<EnergyGenAbilityDB>();
                double stored = power.EnergyStored["electricity"];
                double demand = power.Demand;
                double sustain = move.SustainDemandKW;
                DateTime arrival = move.PredictedExitTime;
                int shipId = leg.Ship.Id;
                Assert.IsTrue(move.ChargedSustain);
                Assert.That(sustain, Is.EqualTo(SustainKw).Within(1e-6));
                Assert.IsTrue(HasWarpArrival(_starSys, shipId, arrival));

                nextId = EntityIDGenerator.NextId;
                cargoIds = Game.EntityIDCounter;
                string json = Game.Save(_game);
                EntityIDGenerator.NextId = 0;
                Game.EntityIDCounter = 0;
                var loaded = Game.Load(json);
                loaded.Settings.EnforceSingleThread = true;
                loaded.Settings.StrictNewtonion = false;

                Assert.IsTrue(TryFind(loaded, shipId, out var loadedSys, out var loadedShip));
                var loadedMove = loadedShip.GetDataBlob<WarpMovingDB>();
                var loadedPower = loadedShip.GetDataBlob<EnergyGenAbilityDB>();
                Assert.IsTrue(loadedMove.ChargedSustain);
                Assert.That(loadedMove.SustainDemandKW, Is.EqualTo(sustain).Within(1e-6));
                Assert.AreEqual(stored, loadedPower.EnergyStored["electricity"]);
                Assert.That(loadedPower.Demand, Is.EqualTo(demand).Within(1e-6));
                Assert.IsTrue(HasWarpArrival(loadedSys, shipId, arrival));

                loaded.TimePulse.TimeStep(arrival);
                Assert.IsNull(loaded.TimePulse.LastFault, loaded.TimePulse.LastFault?.Report);

                Assert.IsFalse(loadedShip.HasDataBlob<WarpMovingDB>(), "the bubble is gone after arrival");
                var after = loadedShip.GetDataBlob<EnergyGenAbilityDB>();
                Assert.That(after.Demand, Is.EqualTo(demand - sustain).Within(1e-6));
                Assert.Greater(after.EnergyStored["electricity"], stored - leg.CreationCost,
                    "arrival must not charge bubble creation again");
            }
            finally
            {
                if (Game.EntityIDCounter < cargoIds)
                    Game.EntityIDCounter = cargoIds;
                if (EntityIDGenerator.NextId < nextId)
                    EntityIDGenerator.NextId = nextId;
            }
        }

        static string PlantHoldsLine(double drive, double cruise)
        {
            return "Drive " + Stringify.Velocity(drive)
                + ". Plant holds " + Stringify.Velocity(cruise)
                + " uphill. Warp at " + Stringify.Velocity(cruise) + ".";
        }

        static (Entity ship, Entity target) RadialLeg(
            double shipAu, double targetAu, double mass, double drive, double outputKw, double coupling, double bleed)
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            double au = Distance.AuToMt(1);
            var epoch = _starSys.StarSysDateTime;
            var ship = AddBody(
                new Vector3(shipAu * au, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(mass, 20),
                WarpBlob(drive, coupling, bleed),
                PowerBlob(epoch, outputKw));
            ship.GetDataBlob<PositionDB>().SetParent(sol);

            var target = AddBody(new Vector3(targetAu * au, 0, 0), new NameDB("mark"));
            return (ship, target);
        }

        static WarpAbilityDB WarpBlob(double drive, double coupling, double bleed)
        {
            return new WarpAbilityDB
            {
                MaxSpeed = drive,
                EnergyType = "electricity",
                BubbleCreationCost = 1000,
                BubbleSustainCost = SustainKw,
                BubbleCollapseCost = 0,
                Coupling = coupling,
                BleedFraction = bleed
            };
        }

        static EnergyGenAbilityDB PowerBlob(DateTime epoch, double outputKw)
        {
            var energy = new EnergyGenAbilityDB(epoch)
            {
                EnergyType = new TestElectricity(),
                MaxOutputFromReactor = outputKw
            };
            energy.EnergyStored["electricity"] = 1e12;
            energy.EnergyStoreMax["electricity"] = 1e12;
            return energy;
        }

        /// <summary>
        /// Earth and Mars on the same side. The free cruise is the same leg with coupling 0.
        /// </summary>
        static EarthMarsLeg OpenEarthMars(double coupling)
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, 1.0, epoch);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, 1.524, epoch);
            var faction = FactionFactory.CreateFaction(_game, "sustain-" + Guid.NewGuid().ToString("N"));
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, epoch);
            double leo = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var ship = AddBody(
                earthAbs + new Vector3(leo, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(ShipMassKg, 20),
                WarpBlob(DriveMps, coupling: 0, bleed: 0),
                PowerBlob(epoch, PlantKw),
                new ActionQueueDB(),
                new NameDB("sustain-ship"));
            ship.FactionOwnerID = faction.Id;
            ship.GetDataBlob<PositionDB>().SetParent(earth);
            ship.SetDataBlob(OrbitDB.FromPosition(earth, ship, epoch));
            OrbitProcessor.ProcessEntity(ship, epoch);

            var departure = MoveMath.GetRelativeState(ship).Velocity;
            var offset = MovePlanner.PlannedWarpExitOffset(ship, mars, departure);
            var free = WarpMath.PriceCruise(ship, mars, epoch, offset);
            ship.GetDataBlob<WarpAbilityDB>().Coupling = coupling;

            var action = WarpMoveAction.CreateWarpOnly(ship, mars, epoch, offset);
            ship.GetDataBlob<ActionQueueDB>().Enqueue(action);
            action.Execute(epoch);

            Assert.IsTrue(ship.HasDataBlob<WarpMovingDB>(), "the bubble should open");
            Assert.IsTrue(ship.GetDataBlob<WarpMovingDB>().HasStarted);
            double freeSeconds = (free.exitTime - epoch).TotalSeconds;
            return new EarthMarsLeg(ship, action, freeSeconds, 1000);
        }

        readonly struct EarthMarsLeg
        {
            public EarthMarsLeg(Entity ship, WarpMoveAction action, double freeSeconds, double creationCost)
            {
                Ship = ship;
                Action = action;
                FreeSeconds = freeSeconds;
                CreationCost = creationCost;
            }

            public Entity Ship { get; }
            public WarpMoveAction Action { get; }
            public double FreeSeconds { get; }
            public double CreationCost { get; }
        }

        static Entity AddOrbitingBody(Entity parent, double mass, double radius_m, double smaAu, DateTime epoch)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, epoch);
            var body = AddBody(
                Vector3.Zero,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB());
            OrbitProcessor.ProcessEntity(body, epoch);
            return body;
        }

        static Entity AddBody(Vector3 absolutePos, params BaseDataBlob[] blobs)
        {
            var ent = Entity.Create();
            var pos = new PositionDB { AbsolutePosition = absolutePos };
            var all = new BaseDataBlob[blobs.Length + 1];
            all[0] = pos;
            Array.Copy(blobs, 0, all, 1, blobs.Length);
            _starSys.AddEntity(ent, all);
            return ent;
        }

        static bool TryFind(Game game, int entityId, out StarSystem system, out Entity entity)
        {
            foreach (var candidate in game.Systems)
            {
                if (candidate.TryGetEntityById(entityId, out entity))
                {
                    system = candidate;
                    return true;
                }
            }
            system = null;
            entity = null;
            return false;
        }

        static bool HasWarpArrival(EntityManager manager, int entityId, DateTime when)
        {
            foreach (var entry in manager.ManagerSubpulses.InstanceProcessorsQueue)
            {
                var (processor, entity) = entry.Item;
                if (processor == nameof(WarpMoveProcessor) && entity.Id == entityId && entry.Time == when)
                    return true;
            }
            return false;
        }

        class TestElectricity : ICargoable
        {
            public int ID => 0;
            public string UniqueID => "electricity";
            public string CargoTypeID => "energy";
            public string Name => "electricity";
            public long MassPerUnit => 0;
            public double VolumePerUnit => 0;
        }
    }
}
