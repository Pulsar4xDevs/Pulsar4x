using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Pulsar4X.ECSLib
{
    public class Engine_Comms
    {
        Dictionary<Guid, Queue<Message>> _MessagesforUI = new Dictionary<Guid, Queue<Message>>();

        Queue<Message> Messagequeue(Guid factionID)
        {
            return _MessagesforUI[factionID];
        }

        //public Message Messageforfaction(Guid factionID)        
        //{
        //    Queue<Message> queue = Messagequeue(factionID);
        //
        //    return queue.Take(1);
        //}

    }

    public class Message
    {

    }
}
