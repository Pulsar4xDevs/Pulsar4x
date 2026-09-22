using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
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
    /// Fleet <see cref="GoalType.ScanAnomalies"/> on Sol: two grav-surveyors + a tanker.
    /// In-game the player clicks a gravitational anomaly (<see cref="GravSurveyCommand"/>);
    /// targeting the star is the planner's fleet-wide path (anomalies as
    /// <see cref="PositionDB"/> children). Factory rings are not parented; this fixture
    /// parents them so that path runs. Tanker is not tasked (no JP-survey ability;
    /// grav planner does not emit MoveTo).
    /// </summary>
    public class GravSurveyFleetTests
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
        public void JPSurveyOrder_NameAndDetails_UseOwnerFactionProgress()
        {
            var scene = BuildSolGravSurveyFleet();
            var order = new JPSurveyOrder(scene.SurveyorA, scene.AnomalyInner);

            Assert.That(order.Details, Is.EqualTo("0%"));
            Assert.That(order.Name, Does.Contain("(0%)"));

            var jp = scene.AnomalyInner.GetDataBlob<JPSurveyableDB>();
            jp.SurveyPointsRemaining[scene.Faction.Id] = jp.PointsRequired / 2;

            Assert.That(order.Details, Is.EqualTo("50%"));
            Assert.That(order.Name, Does.Contain("(50%)"));
        }

        [Test]
        public void ScanAnomalyPlan_AssignsNearestAnomalies_SkipsTanker()
        {
            var scene = BuildSolGravSurveyFleet();

            var planner = new ScanAnomalyPlan();
            var plan = planner.Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            Assert.AreEqual(2, plan.SubGoals.Count,
                "two grav-capable ships; tanker has no JPSurveyAbilityDB");

            var assignedShips = plan.SubGoals.Select(s => s.Sub.Id).ToHashSet();
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorA.Id), "Surveyor A should be tasked");
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorB.Id), "Surveyor B should be tasked");
            Assert.IsFalse(assignedShips.Contains(scene.Tanker.Id),
                "tanker is not a grav surveyor; ScanAnomalyPlan must not task it");

            Assert.IsTrue(plan.SubGoals.All(s => s.Goal.Type == GoalType.ScanAnomalies));
            var assignedPois = plan.SubGoals.Select(s => s.Goal.TargetEntityID).ToHashSet();
            Assert.IsTrue(assignedPois.Contains(scene.AnomalyInner.Id),
                "nearest remaining anomaly (2 AU) is assigned first");
            Assert.IsTrue(assignedPois.Contains(scene.AnomalyMid.Id),
                "next-nearest (4 AU) goes to the second ship");
            Assert.IsFalse(assignedPois.Contains(scene.AnomalyOuter.Id),
                "outer anomaly waits");
        }

        [Test, Timeout(15000)]
        public void FleetGravSurvey_OnSite_CompletesAssignedAnomalies()
        {
            // Two sites only so a finished ship is not immediately handed the next
            // anomaly (warp-to-None still 0-spans). This test is the survey order.
            var scene = BuildSolGravSurveyFleet(onSite: true, includeOuter: false);

            AgentProcessor.AssignGoal(scene.Fleet, scene.Goal);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, scene.Goal.Message);

            // JPSurveyOrder.Execute attaches JPSurveyDB; the processor's first tick is
            // FirstRunOffset 1 h, then Speed 500 finishes a 400-pt site in one hour.
            var wall = Stopwatch.StartNew();
            _sys.ManagerSubpulses.ProcessSystem(_epoch + TimeSpan.FromHours(3));
            Assert.Less(wall.ElapsedMilliseconds, 10000,
                "3h ProcessSystem hung. " + Describe(scene));

            string dump = Describe(scene);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, dump);
            Assert.IsTrue(scene.AnomalyInner.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(scene.Faction.Id),
                "inner anomaly should be surveyed. " + dump);
            Assert.IsTrue(scene.AnomalyMid.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(scene.Faction.Id),
                "mid anomaly should be surveyed. " + dump);
            Assert.IsTrue(SurveyOrderFinished(scene.SurveyorA), "Surveyor A order should have finished. " + dump);
            Assert.IsTrue(SurveyOrderFinished(scene.SurveyorB), "Surveyor B order should have finished. " + dump);

            Assert.IsFalse(HasScanSubgoal(scene.Tanker, scene.Goal),
                "tanker is support, not a grav surveyor. " + dump);

            int wakesA = CountAgentInterrupts(scene.SurveyorA);
            int wakesB = CountAgentInterrupts(scene.SurveyorB);
            Assert.Less(wakesA, 8, $"Surveyor A AgentProcessor flood ({wakesA}). " + dump);
            Assert.Less(wakesB, 8, $"Surveyor B AgentProcessor flood ({wakesB}). " + dump);
        }

        Scene BuildSolGravSurveyFleet(bool onSite = false, bool includeOuter = true)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            sol.SetDataBlob(new StarInfoDB());
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, smaAu: 1.0);

            // Same construction as JPSurveyFactory (points, MoveTypes.None, tiny mass).
            // Parent to Sol so ScanAnomalyPlan's star+children path matches its comment.
            var inner = AddAnomaly(sol, "Anomaly Inner", smaAu: 2.0);
            var mid = AddAnomaly(sol, "Anomaly Mid", smaAu: 4.0);
            Entity outer = null!;
            if (includeOuter)
            {
                outer = AddAnomaly(sol, "Anomaly Outer", smaAu: 8.0);
                Assert.Contains(outer, sol.GetDataBlob<PositionDB>().Children.ToList());
            }

            Assert.Contains(inner, sol.GetDataBlob<PositionDB>().Children.ToList(),
                "inner anomaly must be a PositionDB child of Sol for PlanSubGoals");
            Assert.Contains(mid, sol.GetDataBlob<PositionDB>().Children.ToList());

            var faction = FactionFactory.CreateFaction(Game, "grav-" + Guid.NewGuid().ToString("N"));
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);

            Entity surveyorA, surveyorB;
            if (onSite)
            {
                // Skip warp: sit on the site so JPSurveyOrder can Execute and the
                // hourly processor can finish. Warp-to-None currently 0-spans.
                // Spawn parented to Earth (has OrbitDB) then park; anomalies have no OrbitDB.
                surveyorA = MakeSurveyShip(earth, inner.GetDataBlob<PositionDB>().AbsolutePosition, faction, "Grav Surveyor A");
                ParkAtSite(surveyorA, inner);
                surveyorB = MakeSurveyShip(earth, mid.GetDataBlob<PositionDB>().AbsolutePosition, faction, "Grav Surveyor B");
                ParkAtSite(surveyorB, mid);
            }
            else
            {
                surveyorA = MakeSurveyShip(earth, earthAbs + new Vector3(leoR, 0, 0), faction, "Grav Surveyor A");
                surveyorB = MakeSurveyShip(earth, earthAbs + new Vector3(0, leoR, 0), faction, "Grav Surveyor B");
            }
            var tanker = MakeTanker(earth, earthAbs + new Vector3(-leoR, 0, 0), faction, "Tanker");

            var fleet = FleetFactory.Create(_sys, faction.Id, "Sol Grav Flotilla");
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorA);
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorB);
            fleet.GetDataBlob<FleetDB>().AddChild(tanker);

            return new Scene
            {
                Faction = faction,
                Fleet = fleet,
                Goal = new Goal(GoalType.ScanAnomalies) { TargetEntityID = sol.Id },
                SurveyorA = surveyorA,
                SurveyorB = surveyorB,
                Tanker = tanker,
                Sol = sol,
                AnomalyInner = inner,
                AnomalyMid = mid,
                AnomalyOuter = outer,
            };
        }

        Entity AddBody(Entity parent, string name, double mass, double radius_m, double smaAu)
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
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        Entity AddAnomaly(Entity star, string name, double smaAu)
        {
            double r = Distance.AuToMt(smaAu);
            var pos = new PositionDB(r, 0, 0);
            pos.MoveType = PositionDB.MoveTypes.None;
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1, 1),
                new NameDB(name),
                new JPSurveyableDB(400, new SafeDictionary<int, uint>(), 10_000_000),
                new VisibleByDefaultDB(),
            });
            pos.SetParent(star);
            return ent;
        }

        Entity MakeSurveyShip(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var ship = MakeFueledWarpShip(parent, abs, faction, name);
            // Processor subtracts Speed once per hourly hotloop; 500 finishes a 400-pt site in one tick.
            ship.SetDataBlob(new JPSurveyAbilityDB { Speed = 500 });
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

        static bool HasScanSubgoal(Entity ship, Goal parent)
        {
            if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                return false;
            return goals.ActiveGoal.ParentGoalId == parent.Id
                   && goals.ActiveGoal.Type == GoalType.ScanAnomalies;
        }

        static void ParkAtSite(Entity ship, Entity site)
        {
            if (ship.HasDataBlob<OrbitDB>())
                ship.RemoveDataBlob<OrbitDB>();
            var pos = ship.GetDataBlob<PositionDB>();
            pos.SetParent(site);
            pos.RelativePosition = Vector3.Zero;
        }

        int CountAgentInterrupts(Entity ship)
        {
            int n = 0;
            foreach (var qi in _sys.ManagerSubpulses.InstanceProcessorsQueue)
            {
                if (qi.Item.Item1 == nameof(AgentProcessor) && qi.Item.Item2.Id == ship.Id)
                    n++;
            }
            return n;
        }

        static bool SurveyOrderFinished(Entity ship)
        {
            if (ship.HasDataBlob<JPSurveyDB>())
                return false;
            if (!ship.TryGetDataBlob<ActionQueueDB>(out var q))
                return true;
            var surveys = q.ActionList.OfType<JPSurveyOrder>().ToList();
            if (surveys.Count == 0)
                return true;
            return surveys.All(a => a.Status == ActionStatus.Succeeded || a.IsFinished());
        }

        string Describe(Scene scene)
        {
            string Site(Entity e)
            {
                e.TryGetDataBlob<JPSurveyableDB>(out var j);
                var done = j != null && j.IsSurveyComplete(scene.Fleet.FactionOwnerID);
                var left = j != null && j.SurveyPointsRemaining.TryGetValue(scene.Fleet.FactionOwnerID, out var v)
                    ? v : j?.PointsRequired;
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
                bool surveying = s.HasDataBlob<JPSurveyDB>();
                return $"{n} [{g}] jpdb={surveying} queue=[{q}]";
            }

            string sites = $"{Site(scene.AnomalyInner)}; {Site(scene.AnomalyMid)}";
            if (scene.AnomalyOuter != null)
                sites += $"; {Site(scene.AnomalyOuter)}";
            return $"t={_sys.StarSysDateTime:u} fleetGoal={scene.Goal.Status} '{scene.Goal.Message}' "
                   + $"{sites} | {Ship(scene.SurveyorA)}; {Ship(scene.SurveyorB)}; {Ship(scene.Tanker)}";
        }

        class Scene
        {
            public Entity Faction = null!;
            public Entity Fleet = null!;
            public Goal Goal = null!;
            public Entity SurveyorA = null!;
            public Entity SurveyorB = null!;
            public Entity Tanker = null!;
            public Entity Sol = null!;
            public Entity AnomalyInner = null!;
            public Entity AnomalyMid = null!;
            public Entity AnomalyOuter = null!;
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
