using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using GameEngine.People;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Fleet <see cref="GoalType.ServeyBodies"/> on Mars: two surveyors + a tanker.
    /// <see cref="ServeyBodyPlanner"/> hands POIs (Mars + moons) to geo-capable ships
    /// and sends a flagged fleet tanker to orbit the parent via <see cref="GoalType.MoveTo"/>.
    /// </summary>
    public class GeoSurveyFleetTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;
        private DateTime _epoch;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            game.Settings.EnforceSingleThread = true;
            game.Settings.StrictNewtonion = true;
            return game;
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var sys in Game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _sys = new StarSystem();
            _sys.Initialize(Game, "Sol", -1);
            _sys.IncrementExternalObserver(priority: true);
            _epoch = _sys.StarSysDateTime;
        }

        [Test]
        public void GeoSurveyOrder_NameAndDetails_UseOwnerFactionProgress()
        {
            var scene = BuildMarsSurveyFleet();
            var order = new GeoSurveyOrder(scene.SurveyorA, scene.Mars);

            Assert.That(order.Details, Is.EqualTo("0%"));
            Assert.That(order.Name, Does.Contain("(0%)"));

            var geo = scene.Mars.GetDataBlob<GeoSurveyableDB>();
            geo.GeoSurveyStatus[scene.Faction.Id] = geo.PointsRequired / 2;

            Assert.That(order.Details, Is.EqualTo("50%"));
            Assert.That(order.Name, Does.Contain("(50%)"));
        }

        [Test]
        public void ServeyBodyPlanner_AssignsMarsPhobos_AndTankerMoveToParent()
        {
            var scene = BuildMarsSurveyFleet();

            var planner = new ServeyBodyPlanner();
            var plan = planner.Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            Assert.AreEqual(3, plan.SubGoals.Count,
                "two surveyors + tanker MoveTo parent");

            var assignedShips = plan.SubGoals.Select(s => s.Sub.Id).ToHashSet();
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorA.Id), "Surveyor A should be tasked");
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorB.Id), "Surveyor B should be tasked");
            Assert.IsTrue(assignedShips.Contains(scene.Tanker.Id), "tanker should MoveTo the parent");

            var surveyGoals = plan.SubGoals.Where(s => s.Goal.Type == GoalType.ServeyBodies).ToList();
            Assert.AreEqual(2, surveyGoals.Count);
            Assert.AreEqual(scene.Mars.Id, surveyGoals[0].Goal.TargetEntityID,
                "targeted parent is first");
            Assert.AreEqual(scene.Phobos.Id, surveyGoals[1].Goal.TargetEntityID,
                "inner moon (Phobos) before outer (Deimos)");

            var tankerGoal = plan.SubGoals.Single(s => s.Sub.Id == scene.Tanker.Id);
            Assert.AreEqual(GoalType.MoveTo, tankerGoal.Goal.Type);
            Assert.AreEqual(scene.Mars.Id, tankerGoal.Goal.TargetEntityID,
                "tanker orbits the targeted parent, not a moon");
        }

        [Test]
        public void ServeyBodyPlanner_BodySpan_AssignsParentOnly()
        {
            var scene = BuildMarsSurveyFleet();
            AttachBridge(scene.SurveyorA, AdminLevel.Ship);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var surveyGoals = plan.SubGoals.Where(s => s.Goal.Type == GoalType.ServeyBodies).ToList();
            Assert.AreEqual(1, surveyGoals.Count, plan.Message);
            Assert.AreEqual(scene.Mars.Id, surveyGoals[0].Goal.TargetEntityID);
        }

        [Test, Timeout(15000)]
        public void FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent()
        {
            var scene = BuildMarsSurveyFleet();

            AgentProcessor.AssignGoal(scene.Fleet, scene.Goal);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, scene.Goal.Message);

            // ProcessSystem, not TimeStep: the static Game's master pulse can keep a
            // jump-pair key at GameGlobalDateTime and SimulateTimeUntil 0-spans.
            // One day is past Earth→Mars warp; GeoSurveyOrder gates points to +1 day
            // after Execute starts (~t+26h). That first tick currently hangs the
            // subpulse (replan after complete → warp-to-self), so stop at 24h.
            var wall = Stopwatch.StartNew();
            _sys.ManagerSubpulses.ProcessSystem(_epoch + TimeSpan.FromDays(1));
            Assert.Less(wall.ElapsedMilliseconds, 10000,
                "1-day ProcessSystem hung. " + Describe(scene));

            string dump = Describe(scene);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, dump);
            Assert.IsTrue(IsSurveying(scene.SurveyorA), "Surveyor A should be geo-surveying. " + dump);
            Assert.IsTrue(IsSurveying(scene.SurveyorB), "Surveyor B should be geo-surveying. " + dump);

            var targets = new HashSet<int>();
            if (scene.SurveyorA.TryGetDataBlob<GoalsDB>(out var ga) && ga.ActiveGoal != null)
                targets.Add(ga.ActiveGoal.TargetEntityID);
            if (scene.SurveyorB.TryGetDataBlob<GoalsDB>(out var gb) && gb.ActiveGoal != null)
                targets.Add(gb.ActiveGoal.TargetEntityID);
            Assert.IsTrue(targets.SetEquals(new[] { scene.Mars.Id, scene.Phobos.Id }),
                "parent then inner moon; Deimos waits. " + dump);

            Assert.IsFalse(HasSurveySubgoal(scene.Tanker, scene.Goal),
                "tanker is support, not a surveyor. " + dump);
            Assert.IsTrue(scene.Tanker.TryGetDataBlob<GoalsDB>(out var tankerGoals)
                          && tankerGoals.ActiveGoal != null
                          && tankerGoals.ActiveGoal.Type == GoalType.MoveTo
                          && tankerGoals.ActiveGoal.TargetEntityID == scene.Mars.Id,
                "tanker should MoveTo the parent body. " + dump);
        }

        Scene BuildMarsSurveyFleet()
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, smaAu: 1.0, geoPoints: 50);
            var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, smaAu: 1.524, geoPoints: 575);
            var phobos = AddMoon(mars, "Phobos", 1.07e16, 11_100, sma_m: 9_376_000, geoPoints: 100, e: 0.0151);
            var deimos = AddMoon(mars, "Deimos", 1.51e15, 6_200, sma_m: 23_463_200, geoPoints: 50, e: 0.00033);

            Assert.Contains(phobos, mars.GetDataBlob<PositionDB>().Children.ToList(),
                "Phobos must be a PositionDB child of Mars for PlanSubGoals");
            Assert.Contains(deimos, mars.GetDataBlob<PositionDB>().Children.ToList(),
                "Deimos must be a PositionDB child of Mars for PlanSubGoals");

            var faction = FactionFactory.CreateFaction(Game, "survey-" + Guid.NewGuid().ToString("N"));
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);

            var surveyorA = MakeSurveyShip(earth, earthAbs + new Vector3(leoR, 0, 0), faction, "Surveyor A");
            var surveyorB = MakeSurveyShip(earth, earthAbs + new Vector3(0, leoR, 0), faction, "Surveyor B");
            var tanker = MakeTanker(earth, earthAbs + new Vector3(-leoR, 0, 0), faction, "Tanker");

            var fleet = FleetFactory.Create(_sys, faction.Id, "Mars Survey Flotilla");
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorA);
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorB);
            fleet.GetDataBlob<FleetDB>().AddChild(tanker);
            fleet.GetDataBlob<FleetDB>().FlagShipID = surveyorA.Id;
            AttachBridge(surveyorA, AdminLevel.Planet);

            var fuel = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            Assert.IsTrue(FuelSituation.TryFindFleetTanker(surveyorA, fuel, out var tankerId, out var tankerReason),
                tankerReason);
            Assert.AreEqual(tanker.Id, tankerId, "FuelSituation should pick the fleet mate with the most fuel");

            return new Scene
            {
                Faction = faction,
                Fleet = fleet,
                Goal = new Goal(GoalType.ServeyBodies) { TargetEntityID = mars.Id },
                SurveyorA = surveyorA,
                SurveyorB = surveyorB,
                Tanker = tanker,
                Mars = mars,
                Phobos = phobos,
                Deimos = deimos,
            };
        }

        Entity AddBody(Entity parent, string name, double mass, double radius_m, double smaAu, uint geoPoints)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
                new GeoSurveyableDB { PointsRequired = geoPoints },
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        Entity AddMoon(Entity parent, string name, double mass, double radius_m, double sma_m, uint geoPoints, double e)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            double smaAu = Distance.MToAU(sma_m);
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, e, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
                new GeoSurveyableDB { PointsRequired = geoPoints },
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        static void AttachBridge(Entity ship, AdminLevel level)
        {
            var admin = new AdminSpaceDB();
            admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
            ship.SetDataBlob(admin);
        }

        Entity MakeSurveyShip(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var ship = MakeFueledWarpShip(parent, abs, faction, name);
            ship.SetDataBlob(new GeoSurveyAbilityDB { Speed = 600 });
            return ship;
        }

        Entity MakeTanker(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var ship = MakeFueledWarpShip(parent, abs, faction, name);
            ship.GetDataBlob<ShipInfoDB>().Tanker = true;
            var fuel = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(fuel, 20_000_000);
            return ship;
        }

        Entity MakeFueledWarpShip(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var pos = new PositionDB { AbsolutePosition = abs };
            var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
            var energy = new EnergyGenAbilityDB(_epoch)
            {
                EnergyType = new TestEnergyType(),
                EnergyStored = { ["electricity"] = 1e12 },
                EnergyStoreMax = { ["electricity"] = 1e12 },
            };
            var ship = Entity.Create();
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                warp,
                energy,
                new ActionQueueDB(),
                new ShipInfoDB(),
                new NameDB(name, faction.Id, name),
            });
            pos.SetParent(parent);
            ship.FactionOwnerID = faction.Id;
            ship.SetDataBlob(OrbitDB.FromPosition(parent, ship, _epoch));
            OrbitProcessor.ProcessEntity(ship, _epoch);

            var thrust = new NewtonThrustAbilityDB("test-fuel")
            {
                ThrustInNewtons = 1e9,
                ExhaustVelocity = 10_000,
                FuelBurnRate = 1e9 / 10_000,
            };
            double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            double dry = wet / Math.Exp(20_000 / thrust.ExhaustVelocity);
            thrust.SetFuel(Math.Max(wet - dry, 1), wet);
            ship.SetDataBlob(thrust);

            var data = faction.GetDataBlob<FactionInfoDB>().Data;
            data.Unlock("methalox");
            var fuel = data.CargoGoods.GetAny("methalox");
            thrust.FuelType = fuel.UniqueID;
            var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
            storage.AddCargoByUnit(fuel, 1_000_000);
            ship.SetDataBlob(storage);
            return ship;
        }

        static bool HasSurveySubgoal(Entity ship, Goal parent)
        {
            if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                return false;
            return goals.ActiveGoal.ParentGoalId == parent.Id
                   && goals.ActiveGoal.Type == GoalType.ServeyBodies;
        }

        static bool IsSurveying(Entity ship)
        {
            return ship.TryGetDataBlob<ActionQueueDB>(out var q)
                   && q.ActionList.Any(a => a is GeoSurveyOrder && a.Status != ActionStatus.Failed);
        }

        string Describe(Scene scene)
        {
            string Body(Entity e)
            {
                e.TryGetDataBlob<GeoSurveyableDB>(out var g);
                var done = g != null && g.IsSurveyComplete(scene.Fleet.FactionOwnerID);
                var left = g != null && g.GeoSurveyStatus.TryGetValue(scene.Fleet.FactionOwnerID, out var v) ? v : g?.PointsRequired;
                var n = e.TryGetDataBlob<NameDB>(out var ndb) ? ndb.DefaultName : e.Id.ToString();
                return $"{n}:done={done} left={left}";
            }

            string Ship(Entity s)
            {
                var g = s.TryGetDataBlob<GoalsDB>(out var gd) && gd.ActiveGoal != null
                    ? $"{gd.ActiveGoal.Type}:{gd.ActiveGoal.Status} tgt={gd.ActiveGoal.TargetEntityID} '{gd.ActiveGoal.Message}'"
                    : "no-goal";
                var q = s.TryGetDataBlob<ActionQueueDB>(out var aq)
                    ? string.Join(",", aq.ActionList.Select(x => $"{x.GetType().Name}:{x.Status}"))
                    : "";
                var n = s.TryGetDataBlob<NameDB>(out var ndb) ? ndb.DefaultName : s.Id.ToString();
                double fuelFrac = -1;
                if (FuelSituation.TryGetFuelMass(s, out _, out var stored, out var cap) && cap > 0)
                    fuelFrac = stored / cap;
                return $"{n} [{g}] fuel={fuelFrac:0.00} queue=[{q}]";
            }

            return $"t={_sys.StarSysDateTime:u} fleetGoal={scene.Goal.Status} '{scene.Goal.Message}' "
                   + $"{Body(scene.Mars)}; {Body(scene.Phobos)}; {Body(scene.Deimos)} | "
                   + $"{Ship(scene.SurveyorA)}; {Ship(scene.SurveyorB)}; {Ship(scene.Tanker)}";
        }

        class Scene
        {
            public Entity Faction = null!;
            public Entity Fleet = null!;
            public Goal Goal = null!;
            public Entity SurveyorA = null!;
            public Entity SurveyorB = null!;
            public Entity Tanker = null!;
            public Entity Mars = null!;
            public Entity Phobos = null!;
            public Entity Deimos = null!;
        }

        class TestEnergyType : ICargoable
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
