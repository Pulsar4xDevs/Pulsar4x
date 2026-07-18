using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Messaging;

namespace GameEngine.Engine.Orders
{
    internal class StandAloneOrderHandler : IOrderHandler
    {
        internal StandAloneOrderHandler(Game game)
        {
            Game = game;
        }

        public Game Game { get; private set; }

        public bool HandleOrder(EntityAction entityAction)
        {
            if (entityAction.IsValidCommand(Game))
            {
                if (entityAction.UseActionLanes)
                {
                    if (entityAction.ActionOnDate > entityAction.EntityCommanding.StarSysDateTime)
                    {
                        entityAction.EntityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(entityAction.ActionOnDate, nameof(ActionQueueProcessor), entityAction.EntityCommanding);
                    }

                    if(entityAction.EntityCommanding.TryGetDataBlob<ActionQueueDB>(out var orderableDB))
                    {
                        if(orderableDB.OwningEntity == null) throw new NullReferenceException("orderableDB.OwningEntity cannot be null");
                        
                        orderableDB.ActionList.Add(entityAction);

                        MessagePublisher.Instance.Publish(Message.Create(
                            MessageTypes.OrdersChanged,
                            entityId: entityAction.EntityCommanding.Id,
                            systemId: entityAction.EntityCommanding.Manager.ManagerID,
                            factionId: entityAction.EntityCommanding.FactionOwnerID));

                        Game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor)).ProcessEntity(orderableDB.OwningEntity, Game.TimePulse.GameGlobalDateTime);
                    }
                }
                else
                {
                    if(entityAction.EntityCommanding.StarSysDateTime >= entityAction.ActionOnDate)
                        entityAction.Execute(entityAction.EntityCommanding.StarSysDateTime);
                    else
                    {
                        entityAction.EntityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(entityAction.ActionOnDate, nameof(ActionQueueProcessor), entityAction.EntityCommanding);
                    }
                }
                return true;
            }
            return false;
        }
    }
}