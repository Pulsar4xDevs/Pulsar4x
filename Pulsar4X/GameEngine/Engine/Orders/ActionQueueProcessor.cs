using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Interfaces;

namespace GameEngine.Engine.Orders
{

    public class ActionQueueProcessor : IInstanceProcessor, IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(5);

        public Type GetParameterType => typeof(ActionQueueDB);

        private Game _game;

        public void Init(Game game)
        {
            _game = game;
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime atDateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            ProcessEntity(entity, atDateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<ActionQueueDB>();

            foreach (var entity in entitysWithCargoTransfers)
            {
                ProcessEntity(entity, deltaSeconds);
            }

            return entitysWithCargoTransfers.Count;
        }

        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if(entity.TryGetDataBlob<ActionQueueDB>(out var orderableDB))
            {
                int mask = 0;

                foreach(var entityCommand in orderableDB.ActionList)
                {
                    if ((mask & ((int)entityCommand.ActionLanes)) == 0) //bitwise and
                    {
                        if (entityCommand.IsBlocking)
                        {
                            mask = mask | ((int)entityCommand.ActionLanes); //bitwise or
                        }
                        if (atDateTime >= entityCommand.ActionOnDate)
                        {
                            if(entityCommand.PauseOnAction &! entityCommand.IsRunning)
                            {
                                var e = Event.Create(EventType.OrdersHalt,
                                                        atDateTime,
                                                        "",
                                                        entityCommand.RequestingFactionGuid,
                                                        entityCommand.EntityCommanding.Manager.ManagerID,
                                                        entityCommand.EntityCommandingGuid);
                                EventManager.Instance.Publish(e);
                            }
                            entityCommand.Execute(atDateTime);
                        }
                    }
                }

                // Reflect each action's lifecycle into the observable Status the agent layer
                // reads, then auto-clean only *unlinked* (manual/legacy) finished actions.
                // Goal-linked finished actions are kept (as Succeeded) until their owning
                // agent rolls them up and calls ClearFor.
                foreach (var action in orderableDB.ActionList)
                {
                    if (action.Status == ActionStatus.Failed) continue;
                    if (action.IsFinished()) action.Status = ActionStatus.Succeeded;
                    else if (action.IsRunning) action.Status = ActionStatus.Running;
                }

                orderableDB.ActionList.RemoveAll(e => e.IsFinished() && string.IsNullOrEmpty(e.ParentGoalId));
            }
        }
    }
}
