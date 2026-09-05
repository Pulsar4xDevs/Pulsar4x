using System;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Timing contract: a finished blocking action must not hold the lane for the rest of
    /// this queue pass. Warp-end wakes the queue in the same substep; the follow-on burn
    /// has to be allowed to Execute on that pass.
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

        class StubMoveAction : EntityAction
        {
            readonly Entity _entity;
            public bool Finished;
            public bool Running;
            public bool Executed;

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

            internal override bool IsFinished() => Finished;

            public override EntityAction Clone() => throw new NotImplementedException();
        }
    }
}
