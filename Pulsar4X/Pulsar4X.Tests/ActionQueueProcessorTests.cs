using System;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Modding;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Lane contract: a finished blocking action with no ParentGoalId frees the lane so a
    /// hand-queued follow-on can Execute this pass. A goal-tagged finished blocker holds
    /// the lane until the agent drops unstarted follow-ons and Plan()s.
    /// </summary>
    public class ActionQueueProcessorTests
    {
        private Game _game;
        private StarSystem _starSys;

        [SetUp]
        public void SetUp()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            _game = new Game(new NewGameSettings(), modDataStore);
            _starSys = new StarSystem();
            _starSys.Initialize(_game, "Sol", -1);
            _game.Systems.Add(_starSys);
        }

        [Test]
        public void FinishedBlockingAction_DoesNotMaskNextActionOnSamePass()
        {
            var entity = Entity.Create();
            var queue = new ActionQueueDB();
            _starSys.AddEntity(entity, new BaseDataBlob[] { queue });

            var first = new StubMoveAction(entity) { Finished = true };
            var second = new StubMoveAction(entity);
            queue.Enqueue(first);
            queue.Enqueue(second);

            var now = _starSys.StarSysDateTime;
            _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, now);

            Assert.AreEqual(ActionStatus.Succeeded, first.Status);
            Assert.IsTrue(second.Executed, "follow-on movement action must Execute on the same pass once the previous one is finished");
            Assert.IsTrue(second.IsRunning);
        }

        [Test]
        public void GoalTaggedFinishedBlockingAction_DoesNotStartNextSameLaneThisPass()
        {
            var entity = Entity.Create();
            var queue = new ActionQueueDB();
            _starSys.AddEntity(entity, new BaseDataBlob[] { queue });

            const string goalId = "move-goal";
            var first = new StubMoveAction(entity) { Finished = true, ParentGoalId = goalId };
            var second = new StubMoveAction(entity) { ParentGoalId = goalId };
            queue.Enqueue(first);
            queue.Enqueue(second);

            var now = _starSys.StarSysDateTime;
            _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, now);

            Assert.AreEqual(ActionStatus.Succeeded, first.Status);
            Assert.IsFalse(second.Executed,
                "goal-tagged follow-on must wait for the agent to drop unstarted work and Plan()");
        }

        [Test]
        public void RunningBlockingAction_StillMasksNextAction()
        {
            var entity = Entity.Create();
            var queue = new ActionQueueDB();
            _starSys.AddEntity(entity, new BaseDataBlob[] { queue });

            var first = new StubMoveAction(entity) { Running = true };
            var second = new StubMoveAction(entity);
            queue.Enqueue(first);
            queue.Enqueue(second);

            var now = _starSys.StarSysDateTime;
            _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, now);

            Assert.IsFalse(second.Executed, "an unfinished blocking action must still hold the lane");
        }

        [Test]
        public void FailedFinish_StaysFailedAndDoesNotHoldAHandQueue()
        {
            var entity = Entity.Create();
            var queue = new ActionQueueDB();
            _starSys.AddEntity(entity, new BaseDataBlob[] { queue });

            var first = new StubMoveAction(entity) { FailOnFinish = true };
            var second = new StubMoveAction(entity);
            queue.Enqueue(first);
            queue.Enqueue(second);

            var now = _starSys.StarSysDateTime;
            _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, now);

            Assert.AreEqual(ActionStatus.Failed, first.Status);
            Assert.AreEqual("not enough ΔV", first.FailureMessage);
            Assert.IsTrue(queue.ActionList.Contains(first), "a failed line stays so the player can read it");
            Assert.IsTrue(second.Executed, "a goal-less failure does not hold the lane");
        }

        [Test]
        public void FailedGoalAction_FailsTheGoalWithTheReason()
        {
            var entity = Entity.Create();
            var queue = new ActionQueueDB();
            var goals = new GoalsDB();
            var goal = new Goal(GoalType.MoveTo) { Status = GoalStatus.Active };
            goals.ActiveGoal = goal;
            goals.GivenGoal = goal;
            _starSys.AddEntity(entity, new BaseDataBlob[] { queue, goals, new ShipInfoDB() });

            var first = new StubMoveAction(entity) { FailOnFinish = true, ParentGoalId = goal.Id };
            var second = new StubMoveAction(entity) { ParentGoalId = goal.Id };
            queue.Enqueue(first);
            queue.Enqueue(second);

            var now = _starSys.StarSysDateTime;
            _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, now);

            Assert.AreEqual(ActionStatus.Failed, first.Status);
            Assert.AreEqual(GoalStatus.Failed, goal.Status);
            Assert.AreEqual("not enough ΔV", goal.Message);
            Assert.IsFalse(second.Executed, "a failed goal holds the lane until the agent drops the follow-on");
            Assert.AreEqual(0, queue.ActionList.Count);
        }

        class StubMoveAction : EntityAction
        {
            readonly Entity _entity;
            public bool Finished;
            public bool Running;
            public bool Executed;
            public bool FailOnFinish;

            public StubMoveAction(Entity entity)
            {
                _entity = entity;
                ActionOnDate = DateTime.MinValue;
            }

            public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
            public override bool IsBlocking => true;
            public override string Name => "stub";
            public override string Details => "";
            internal override Entity EntityCommanding => _entity;

            internal override bool IsValidCommand(Game game) => true;

            internal override void Execute(DateTime atDateTime)
            {
                Executed = true;
                IsRunning = true;
                Running = true;
            }

            internal override bool IsFinished()
            {
                if (!FailOnFinish)
                    return Finished;
                Fail("not enough ΔV");
                return true;
            }

            public override EntityAction Clone() => throw new NotImplementedException();
        }
    }
}
