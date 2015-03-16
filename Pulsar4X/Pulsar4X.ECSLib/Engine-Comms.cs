using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// this way makes it easier to hide writing to the UI queue from the UI by useing internal;
    /// </summary>
    public class Engine_Comms
    {
        Dictionary<Guid, ConcurrentQueue<Message>> _MessagesforUI = new Dictionary<Guid, ConcurrentQueue<Message>>();
        Dictionary<Guid, ConcurrentQueue<Message>> _MessagesforLib = new Dictionary<Guid, ConcurrentQueue<Message>>();   


        /// <summary>
        /// Pop a message off the UI queue 
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
        public Message UIPop(Guid factionID)
        {
            ConcurrentQueue<Message> queue = _MessagesforUI[factionID];
            Message message =null;
            queue.TryDequeue(out message);
            return message;
        }

        /// <summary>
        /// lib sends a message to the UI queue here
        /// </summary>
        /// <param name="factionID"></param>
        /// <param name="message"></param>
        internal void UIPush(Guid factionID, Message message)
        {
            ConcurrentQueue<Message> queue = _MessagesforUI[factionID];
            queue.Enqueue(message);
        }

        /// <summary>
        /// lib reads messages from the UI here. 
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
        internal Message LibPop(Guid factionID)
        {
            ConcurrentQueue<Message> queue = _MessagesforLib[factionID];
            Message message = null;
            queue.TryDequeue(out message);
            return message;
        }

        /// <summary>
        /// UI writes messages to the lib here. 
        /// </summary>
        /// <param name="factionID"></param>
        /// <param name="message"></param>
        public void LibPush(Guid factionID, Message message)
        {
            ConcurrentQueue<Message> queue = _MessagesforLib[factionID];
            queue.Enqueue(message);
        }
       
    }

    /// <summary>
    /// this needs to be fleshed out.
    /// </summary>
    public class Message
    {

    }


    /// <summary>
    /// slightly different way of doing it vs Engine_Comms this feels a bit tider; 
    /// and probibly makes it easier to add password etc to the requestmessagebook
    /// </summary>
    public class Engine_Comms2
    {
        Dictionary<Guid, MessageBook> Messages = new Dictionary<Guid, MessageBook>();
        public Dictionary<string, Guid> Factions = new Dictionary<string, Guid>();
        internal Engine_Comms2()
        { }

        public void AddFaction(Guid factionID)
        {
            Messages.Add(factionID, new MessageBook(factionID));
            //Factions.Add(FactionEntity.Name, factionID)
        }

        public MessageBook RequestMessagebook(Guid factionID)
        {
            return Messages[factionID];
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class MessageBook
    {
        Guid _FactionID;
        public ConcurrentQueue<Message> MessageQueue { get; set; }
        internal MessageBook(Guid factionID)
        {
            MessageQueue = new ConcurrentQueue<Message>();
            _FactionID = factionID;
        }
        

    }
}
