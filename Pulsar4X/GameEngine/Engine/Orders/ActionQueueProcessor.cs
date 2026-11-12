using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.Names;

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
                // The instance-processor copy is a second Activator instance and is
                // never Init'd, so _game is null on WakeActionQueue / HandleOrder.
                // The hotloop copy is Init'd. Always take Game from the entity.
                var game = entity.Manager?.Game ?? _game;
                // PauseTime cancels the running step's CTS. Yield unstarted work in
                // this same pass so a warp drop-in that just paused does not start
                // circularise at this same instant.
                bool pulseStopping = game != null && game.TimePulse.IsStopping;

                foreach(var entityCommand in orderableDB.ActionList)
                {
                    if(entityCommand.Status == ActionStatus.Succeeded
                        || entityCommand.Status == ActionStatus.Failed)
                        continue;

                    // Processor-side truth can lead Status: warp drop-in sets IsAtTarget
                    // before this pass. Goal-less scripts still free the lane so a
                    // hand-queued follow-on can Execute now. Goal-tagged work holds the
                    // lane until the agent drops unstarted follow-ons and Plan()s.
                    if (entityCommand.IsFinished())
                    {
                        // IsFinished may already have set Failed (short ΔV, and the like).
                        // A success is the only status this loop fills in.
                        if (entityCommand.Status != ActionStatus.Failed)
                            entityCommand.Status = ActionStatus.Succeeded;
                        if (!string.IsNullOrEmpty(entityCommand.ParentGoalId))
                        {
                            wakeAgent = true;
                            if (entityCommand.IsBlocking)
                                mask = mask | ((int)entityCommand.ActionLanes);
                        }
                        // One-shot at finish, only for this order (it was current).
                        // Warp drop-in takes this path; PauseOnAction is left set
                        // through departure so it fires here, not at TryStartWarp.
                        if (entityCommand.PauseOnAction)
                        {
                            HaltOnOneShot(entityCommand, atDateTime);
                            pulseStopping = true;
                        }
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
                            // Not current-and-starting while the pulse is already
                            // stopping: a follower with a past ActionOnDate must not
                            // halt or Execute during the earlier warp's drop-in pass.
                            if (pulseStopping && !entityCommand.IsRunning)
                                continue;

                            if (entityCommand.PauseOnAction && !entityCommand.IsRunning
                                && entityCommand is not WarpMoveAction)
                            {
                                HaltOnOneShot(entityCommand, atDateTime);
                                pulseStopping = true;
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
                        if (action.Status == ActionStatus.Failed)
                        {
                            if (!string.IsNullOrEmpty(action.ParentGoalId))
                                wakeAgent = true;
                        }
                        else if (action.Status != ActionStatus.Succeeded)   // transition
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

                orderableDB.ActionList.RemoveAll(e =>
                    e.Status != ActionStatus.Failed
                    && e.IsFinished()
                    && string.IsNullOrEmpty(e.ParentGoalId));

                if (wakeAgent)//run the agent to check off goal progression.
                    AgentProcessor.RunAgentNow(entity, atDateTime);
            }
        }

        /// <summary>
        /// One-shot PauseOnAction: publish OrdersHalt, pause the pulse ourselves
        /// (OrdersHalt is off by default and is not a standing special case), then
        /// clear the flag so unpause does not re-halt. Does not touch OrdersCompleted.
        /// </summary>
        void HaltOnOneShot(EntityAction action, DateTime atDateTime)
        {
            var commanded = action.EntityCommanding;
            string name = commanded != null && commanded.TryGetDataBlob<NameDB>(out var names)
                ? names.GetName(action.RequestingFactionGuid)
                : "";
            string message = string.IsNullOrEmpty(name) ? action.Name : $"{name}: {action.Name}";
            var ev = Event.Create(
                EventType.OrdersHalt,
                atDateTime,
                message,
                action.RequestingFactionGuid,
                commanded?.Manager?.ManagerID,
                action.EntityCommandingGuid);
            EventManager.Instance.Publish(ev);
            var pulse = (action.EntityCommanding?.Manager?.Game ?? _game)?.TimePulse;
            pulse?.PauseTime(action.RequestingFactionGuid, ev, action.Name);
            action.PauseOnAction = false;
        }
    }
}
