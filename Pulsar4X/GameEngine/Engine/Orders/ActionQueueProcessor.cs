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
                bool wakeAgent = false;

                foreach(var entityCommand in orderableDB.ActionList)
                {
                    if(entityCommand.Status == ActionStatus.Succeeded)
                        continue;

                    // Processor-side truth can lead Status: warp drop-in sets IsAtTarget
                    // before this pass. A finished blocking action must not keep the lane
                    // or the next movement action waits for another queue tick.
                    if (entityCommand.IsFinished())
                    {
                        entityCommand.Status = ActionStatus.Succeeded;
                        if (!string.IsNullOrEmpty(entityCommand.ParentGoalId))
                            wakeAgent = true;
                        continue;
                    }

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
                
                foreach (var action in orderableDB.ActionList)
                {
                    if (action.Status == ActionStatus.Failed)
                    {
                        // Failed is set by the action itself (inside Execute / IsFinished).
                        // We only want the transition, so a simple flag on the action
                        // or a previous-status snapshot is ideal; for a first cut you can
                        // accept the occasional extra wake while a Failed is sitting around.
                        if (!string.IsNullOrEmpty(action.ParentGoalId))
                            wakeAgent = true;
                        continue;
                    }

                    if (action.IsFinished())
                    {
                        if (action.Status != ActionStatus.Succeeded)   // transition
                        {
                            action.Status = ActionStatus.Succeeded;
                            if (!string.IsNullOrEmpty(action.ParentGoalId))
                                wakeAgent = true;
                        }
                    }
                    else if (action.IsRunning)
                    {
                        action.Status = ActionStatus.Running;
                    }
                }

                orderableDB.ActionList.RemoveAll(e => e.IsFinished() && string.IsNullOrEmpty(e.ParentGoalId));

                if (wakeAgent)//run the agent to check off goal progression.
                    AgentProcessor.RunAgentNow(entity);
            }
        }
    }
}
