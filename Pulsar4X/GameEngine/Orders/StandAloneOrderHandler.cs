using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using System;

namespace Pulsar4X.Engine.Orders
{
    internal class StandAloneOrderHandler : IOrderHandler
    {
        internal StandAloneOrderHandler(Game game)
        {
            Game = game;
        }

        public Game Game { get; private set; }

        public void HandleOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(Game))
            {
                if (entityCommand.UseActionLanes)
                {
                    if (entityCommand.ActionOnDate > entityCommand.EntityCommanding.StarSysDateTime)
                    {
                        entityCommand.EntityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(entityCommand.ActionOnDate, nameof(OrderableProcessor), entityCommand.EntityCommanding);
                    }
                    var orderableDB = entityCommand.EntityCommanding.GetDataBlob<OrderableDB>();

                    if(orderableDB == null) throw new NullReferenceException("orderableDB cannot be null");
                    if(orderableDB.OwningEntity == null) throw new NullReferenceException("orderableDB.OwningEntity cannot be null");

                    orderableDB.ActionList.Add(entityCommand);
                    Game.ProcessorManager.GetInstanceProcessor(nameof(OrderableProcessor)).ProcessEntity(orderableDB.OwningEntity, Game.TimePulse.GameGlobalDateTime);
                }
                else
                {
                    if(entityCommand.EntityCommanding.StarSysDateTime >= entityCommand.ActionOnDate)
                        entityCommand.Execute(entityCommand.EntityCommanding.StarSysDateTime);
                    else
                    {
                        entityCommand.EntityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(entityCommand.ActionOnDate, nameof(OrderableProcessor), entityCommand.EntityCommanding);
                    }
                }
            }
        }
    }
}