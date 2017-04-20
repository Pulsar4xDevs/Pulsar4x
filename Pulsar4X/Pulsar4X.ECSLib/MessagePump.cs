using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Pulsar4X.ECSLib.GanttOrders;

namespace Pulsar4X.ECSLib
{
    public class MessagePump
    {
        internal Dictionary<Player, ConcurrentQueue<Message>> MessageOutQueue = new Dictionary<Player, ConcurrentQueue<Message>>();
        internal Dictionary<Player, ConcurrentQueue<Order>> MessageInQueue = new Dictionary<Player, ConcurrentQueue<Order>>();
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

    public interface IOrderableProcessor
    {
        void ProcessOrder(Order order);
        void FirstProcess(Order order);
        // LastProcess(Order order);
        Order GetCurrentOrder(Order order);
        PercentValue GetPercentComplete(Order order);

    }

    public class Order
    {
        //public enum OrderType
        //{
        //    DataRequest,
        //    ObjectOrder
        //}
        //public OrderType TypeOfOrder;
        public Guid EntityGuid { get; set; }
        public Guid FactionID { get; set; }
        public IOrderableProcessor Processor;
        public Guid TargetEntityGuid { get; internal set; }
        public Order StartAfter { get; set; }
        internal Entity ThisEntity { get; private set; }
        internal Entity FactionEntity { get; private set; }
        internal Entity TargetEntity { get; private set; }
        public DateTime EstTimeComplete { get; internal set; }


        public bool IsTargetEntityDependant { get; internal set; }

        internal GanttList OrdersQueueReference { get; set; }

        public Order(IOrderableProcessor processor, Guid entityGuid, Guid factionID)
        {
            Processor = processor;
            EntityGuid = entityGuid;
            FactionID = factionID;
            IsTargetEntityDependant = false;
        }

        public Order(IOrderableProcessor processor, Guid entityGuid, Guid factionID, Guid targetGuid)
        {
            Processor = processor;
            EntityGuid = entityGuid;
            FactionID = factionID;
            TargetEntityGuid = targetGuid;
            IsTargetEntityDependant = true;
        }


        /// <summary>
        /// looks up entity guids, and checks validity.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        internal bool PreProcessing(Game game)
        {
            Entity entity;
            if (!game.GlobalManager.TryGetEntityByGuid(EntityGuid, out entity))
                return false;
            ThisEntity = entity;
            if (!ThisEntity.HasDataBlob<OrderableDB>())
                return false;
            OrdersQueueReference = ThisEntity.GetDataBlob<OrderableDB>().OrdersQueue;
            Entity factionEntity;
            if (!game.GlobalManager.FindEntityByGuid(FactionID, out factionEntity))
                return false;
            FactionEntity = factionEntity;
            if (IsTargetEntityDependant)
            {
                Entity targetEntity;
                if (!game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out targetEntity))
                    return false;
                TargetEntity = targetEntity;
            }
            if (entity.GetDataBlob<OwnedDB>().EntityOwner != FactionEntity)
                return false;

            return true;
        }
    }





}
