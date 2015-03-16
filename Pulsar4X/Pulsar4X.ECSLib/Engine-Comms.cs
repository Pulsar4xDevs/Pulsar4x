using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Pulsar4X.ECSLib
{
    public class Engine_Comms
    {
        Dictionary<Guid, Queue<Message>> _MessagesforUI = new Dictionary<Guid, Queue<Message>>();
        Dictionary<Guid, Queue<Message>> _MessagesforLib = new Dictionary<Guid, Queue<Message>>();



        public Message UIPop(Guid factionID)
        {
            Queue<Message> queue = _MessagesforUI[factionID];
            return queue.Dequeue();
        }

        internal void UIPush(Guid factionID, Message message)
        {
            Queue<Message> queue = _MessagesforUI[factionID];
            queue.Enqueue(message);
        }


        internal Message LibPop(Guid factionID)
        {
            Queue<Message> queue = _MessagesforLib[factionID];
            return queue.Dequeue();
        }

        public void LibPush(Guid factionID, Message message)
        {
            Queue<Message> queue = _MessagesforLib[factionID];
            queue.Enqueue(message);
        }
    }

    public class Message
    {

    }
}
