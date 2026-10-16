using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// One pending <see cref="AgentProcessor"/> wake per entity, and a master-pulse
    /// jump key that is gone once the pulse has run it.
    /// </summary>
    public class AgentWakeTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            game.Settings.EnforceSingleThread = true;
            game.Settings.EnableMultiThreading = false;
            return game;
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var sys in Game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _sys = new StarSystem();
            _sys.Initialize(Game, "Wake System", -1);
            _sys.SetActivityState(SystemActivityState.Foreground);
            Game.TimePulse.EntityDictionary.Clear();
        }

        [Test]
        public void SecondSchedule_ReplacesThePendingWake_AndLeavesWarp()
        {
            var ship = AddShip("skiff");
            var now = _sys.StarSysDateTime;
            var warpAt = now + TimeSpan.FromHours(1);
            var first = now + TimeSpan.FromHours(2);
            var second = now + TimeSpan.FromHours(5);

            _sys.ManagerSubpulses.AddEntityInterupt(warpAt, nameof(WarpMoveProcessor), ship);
            AgentProcessor.ScheduleAgent(ship, first);
            AgentProcessor.ScheduleAgent(ship, second);

            var agents = AgentRows(ship);
            Assert.AreEqual(1, agents.Count);
            Assert.AreEqual(second, agents[0]);
            Assert.AreEqual(1, Count(nameof(WarpMoveProcessor), ship));
            Assert.AreEqual(warpAt, Rows(nameof(WarpMoveProcessor), ship)[0]);
        }

        [Test]
        public void PiledWakes_DrainToOne()
        {
            var ship = AddShip("prospector");
            var now = _sys.StarSysDateTime;
            var pulses = _sys.ManagerSubpulses;
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(1), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(2), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(3), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(4), nameof(WarpMoveProcessor), ship);

            var replacement = now + TimeSpan.FromHours(6);
            AgentProcessor.ScheduleAgent(ship, replacement);

            Assert.AreEqual(1, Count(nameof(AgentProcessor), ship));
            Assert.AreEqual(replacement, AgentRows(ship)[0]);
            Assert.AreEqual(1, Count(nameof(WarpMoveProcessor), ship));
        }

        [Test]
        public void RunningAction_StaysAtOneWakeAcrossPolls()
        {
            var ship = AddShip("miner");
            var now = _sys.StarSysDateTime;
            var goal = new Goal(GoalType.MoveTo) { Status = GoalStatus.Active };
            ship.SetDataBlob(new GoalsDB { ActiveGoal = goal, GivenGoal = goal });
            ship.GetDataBlob<ActionQueueDB>().Enqueue(new HoldAction(ship, finished: false)
            {
                ParentGoalId = goal.Id,
            });

            var pulses = _sys.ManagerSubpulses;
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(1), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(2), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromMinutes(90), nameof(WarpMoveProcessor), ship);

            for (int i = 0; i < 3; i++)
            {
                AgentProcessor.RunAgentNow(ship, now);
                Assert.AreEqual(1, Count(nameof(AgentProcessor), ship), "poll " + i);
            }

            Assert.AreEqual(1, Count(nameof(WarpMoveProcessor), ship));
            Assert.AreEqual(GoalStatus.Active, goal.Status);
        }

        [Test]
        public void FinishedAction_WakesTheAgent_AndLeavesOneWake()
        {
            var ship = AddShip("surveyor");
            var rock = Entity.Create();
            _sys.AddEntity(rock, new BaseDataBlob[] { new NameDB("rock") });
            var now = _sys.StarSysDateTime;
            var goal = new Goal(GoalType.MoveTo)
            {
                Status = GoalStatus.Active,
                TargetEntityID = rock.Id,
            };
            ship.SetDataBlob(new GoalsDB { ActiveGoal = goal, GivenGoal = goal });

            // The finished action is the wake. The one still running makes the
            // replan continue, which is the ScheduleAgent that must replace the pile.
            var queue = ship.GetDataBlob<ActionQueueDB>();
            queue.Enqueue(new HoldAction(ship, finished: true) { ParentGoalId = goal.Id });
            queue.Enqueue(new HoldAction(ship, finished: false, running: true) { ParentGoalId = goal.Id });

            var pulses = _sys.ManagerSubpulses;
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(1), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(2), nameof(AgentProcessor), ship);
            pulses.AddEntityInterupt(now + TimeSpan.FromHours(3), nameof(WarpMoveProcessor), ship);

            Game.ProcessorManager
                .GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(ship, now);

            Assert.AreEqual(1, Count(nameof(AgentProcessor), ship), goal.Status + " " + goal.Message);
            Assert.AreEqual(1, Count(nameof(WarpMoveProcessor), ship));
            Assert.AreEqual(GoalStatus.Active, goal.Status);
        }

        [Test]
        public void JumpKey_IsGoneAfterThePulse_AndTheNextStepMovesOn()
        {
            var ship = AddShip("jumper");
            var start = Game.TimePulse.GameGlobalDateTime;
            var sooner = start + TimeSpan.FromHours(1);
            var keyTime = start + TimeSpan.FromHours(4);

            Game.TimePulse.AddSystemInteractionInterupt(
                keyTime,
                PulseActionEnum.JumpOutProcessor,
                new SystemEntityJumpPair { JumpingEntity = ship, JumpSystem = _sys });

            Game.TimePulse.TimeStep(sooner);
            Assert.AreEqual(sooner, Game.TimePulse.GameGlobalDateTime);
            Assert.IsTrue(Game.TimePulse.EntityDictionary.ContainsKey(keyTime));

            Game.TimePulse.TimeStep(keyTime);
            Assert.AreEqual(keyTime, Game.TimePulse.GameGlobalDateTime);
            Assert.IsFalse(Game.TimePulse.EntityDictionary.ContainsKey(keyTime));

            Game.TimePulse.TimeStep(keyTime + TimeSpan.FromHours(1));
            Assert.Greater(Game.TimePulse.GameGlobalDateTime, keyTime);
            Assert.IsNull(Game.TimePulse.LastFault);
        }

        Entity AddShip(string name)
        {
            var ship = Entity.Create();
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                new ShipInfoDB(),
                new ActionQueueDB(),
                new NameDB(name),
            });
            return ship;
        }

        int Count(string processor, Entity entity)
        {
            return Rows(processor, entity).Count;
        }

        List<DateTime> Rows(string processor, Entity entity)
        {
            var times = new List<DateTime>();
            foreach (var qi in _sys.ManagerSubpulses.InstanceProcessorsQueue)
            {
                if (qi.Item.Item1 == processor && qi.Item.Item2 == entity)
                    times.Add(qi.Time);
            }
            return times;
        }

        List<DateTime> AgentRows(Entity entity) => Rows(nameof(AgentProcessor), entity);

        sealed class HoldAction : EntityAction
        {
            readonly Entity _entity;
            readonly bool _finished;

            public HoldAction(Entity entity, bool finished, bool running = false)
            {
                _entity = entity;
                _finished = finished;
                IsRunning = running;
                if (running)
                    Status = ActionStatus.Running;
                EntityCommandingGuid = entity.Id;
                RequestingFactionGuid = entity.FactionOwnerID;
                ActionOnDate = entity.StarSysDateTime;
            }

            public override ActionLaneTypes ActionLanes => ActionLaneTypes.InteractWithSelf;
            public override bool IsBlocking => true;
            public override string Name => "hold";
            public override string Details => "";
            internal override Entity EntityCommanding => _entity;
            internal override bool IsValidCommand(Game game) => true;
            internal override void Execute(DateTime atDateTime) { }
            internal override bool IsFinished() => _finished;
            public override EntityAction Clone() => new HoldAction(_entity, _finished, IsRunning);
        }
    }
}
