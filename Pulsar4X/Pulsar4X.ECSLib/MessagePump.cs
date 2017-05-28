using System;
using System.Collections.Concurrent;
using System.Collections.Generic;



namespace Pulsar4X.ECSLib
{
    public class MessagePump
    {
        internal Dictionary<Player, ConcurrentQueue<Message>> MessageOutQueue = new Dictionary<Player, ConcurrentQueue<Message>>();
        internal Dictionary<Player, ConcurrentQueue<BaseAction>> MessageInQueue = new Dictionary<Player, ConcurrentQueue<BaseAction>>();
        public MessagePump(Game game)
        {
            foreach(Player player in game.Players) {
                MessageOutQueue.Add(player, new ConcurrentQueue<Message>());
                MessageInQueue.Add(player, new ConcurrentQueue<BaseAction>());
            }
        }


        public void EnqueueMessage(Player toPlayer, Message message)
        {
            MessageOutQueue[toPlayer].Enqueue(message);
        }
        public void EnqueueOrder(Player forPlayer, BaseAction message)
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
        void ProcessOrder(BaseAction order);
        void FirstProcess(BaseAction order);
        // LastProcess(BaseAction order);
        BaseAction GetCurrentOrder(BaseAction order);
        PercentValue GetPercentComplete(BaseAction order);
    }
}
