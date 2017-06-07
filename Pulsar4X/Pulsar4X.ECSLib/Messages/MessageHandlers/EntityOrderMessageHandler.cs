using System;

namespace Pulsar4X.ECSLib
{
    public class EntityOrderMessageHandler : IMessageHandler    
    {
        public bool HandleMessage(Game game, IncomingMessageType messageType, AuthenticationToken authToken, string message)
        {
            if (messageType == IncomingMessageType.EntityOrdersWrite)
            {
                BaseOrder order = OrderSerializer.DeserializeOrder(message);
                
                Entity entity;
                if (game.GlobalManager.FindEntityByGuid(order.EntityGuid, out entity))
                    entity.Manager.OrderQueue.Enqueue(order);                   
                return true;
            }
            return false;
        }
    }
}