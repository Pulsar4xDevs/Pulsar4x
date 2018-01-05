using System;

namespace Pulsar4X.ECSLib
{
    internal interface OrderHandler
    {
        Game Game { get; }

        void HandleOrder(EntityCommand entityCommand);
    }


    internal class StandAloneOrderHandler:OrderHandler
    {
        internal StandAloneOrderHandler(Game game)
        {
            Game = game;
            game.OrderHandler = this;
        }

        public Game Game { get; private set; }

        public void HandleOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(Game))
            {
                entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList.Add(entityCommand);
                var commandList = entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList;
                OrderableProcessor.ProcessOrderList(Game, commandList);
            }
        }
    }

}
