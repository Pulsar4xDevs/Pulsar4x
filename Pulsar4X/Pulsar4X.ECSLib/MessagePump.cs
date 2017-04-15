using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MessagePump
    {
        Dictionary<Player, ConcurrentQueue<Message>> MessageOutQueue = new Dictionary<Player, ConcurrentQueue<Message>>();
        Dictionary<Player, ConcurrentQueue<Order>> MessageInQueue = new Dictionary<Player, ConcurrentQueue<Order>>();
        public MessagePump(Game game)
        {
            foreach(Player player in game.Players) {
                MessageOutQueue.Add(player, new ConcurrentQueue<Message>());
                MessageInQueue.Add(player, new ConcurrentQueue<Order>());
            }
        }


        public void EnqueueMessage(Player toPlayer, Message message)
        {
            MessageOutQueue[toPlayer].Enqueue(message);
        }
        public void EnqueueOrder(Player forPlayer, Order message)
        {
            MessageInQueue[forPlayer].Enqueue(message);
        }
    }

    public struct Message
    {
        public enum MessageType
        {
            EntityChangeNotification,
            DatablobChangeNotification,
            DataMessage
        }

        public MessageType TypeOfMessage;

        Guid EntityGuid;
        string DatablobName; //(or would Type be better?)

        object serialisedDataObject;
        Type dataObjectType;



    }

    public interface OrderableProcessor
    {
        void processOrder(Game game, Order order);
    }

    internal static class OrderProcessor2
    {
        internal static void processOrder(Game game, Order order)
        {

            order.ProcessorName.processOrder(game, order);
        }

    }

    public class Order
    {
        public enum OrderType
        {
            DataRequest,
            ObjectOrder
        }
        public OrderType TypeOfOrder;
        public Guid EntityForOrderReq;
        public OrderableProcessor ProcessorName; //or would a stringname be better?

        public static Order newOrder()
        {
            Order newOrder = new Order();
            newOrder.TypeOfOrder = OrderType.ObjectOrder;
            newOrder.ProcessorName = new ShipMoveOrderProcessor();
            return newOrder;


        }
    }





}
