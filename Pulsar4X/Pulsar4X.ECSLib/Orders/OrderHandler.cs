namespace Pulsar4X.ECSLib
{
    internal abstract class OrderHandler
    {
        internal Game _game;

        internal OrderHandler(Game game)
        { 
            _game = game;
            _game.OrderHandler = this;
        }

        internal abstract void HandleOrder(EntityCommand entityCommand);
    }


    internal class StandAloneOrderHandler:OrderHandler
    {
        internal StandAloneOrderHandler(Game game) : base(game)
        {
        }

        internal override void HandleOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(_game))
            {
                entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList.Add(entityCommand);
                var commandList = entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList;
                OrderableProcessor.ProcessOrderList(_game, commandList);
            }              
        }
    }

}
