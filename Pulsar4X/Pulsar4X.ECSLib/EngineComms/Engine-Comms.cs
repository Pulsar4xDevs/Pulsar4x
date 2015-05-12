using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Engine side of the Lib-UI communications. 
    /// There is a default, faction independent, message queue at Guid.Empty, this queue will always exist.
    /// </summary>
    public class EngineComms
    {
        Dictionary<Guid, MessageBook> Messages = new Dictionary<Guid, MessageBook>();

        /// <summary>
        /// a dictionary of faction names and thier guids. mostly so the ui can request the correct factionid for a given faction name. 
        /// </summary>
        public Dictionary<string, Guid> Factions = new Dictionary<string, Guid>();

        /// <summary>
        /// Engine_comms constructor. 
        /// </summary>
        internal EngineComms()
        {
            // we will add a default message queue, which will use an empty guid so people know how to find it.
            AddFaction(Guid.Empty);
        }

        public void AddFaction(Guid factionID)
        {
            Messages.Add(factionID, new MessageBook(factionID));
        }

        /// <summary>
        /// if a faction is killed off or otherwise removed from the game it needs to be removed from Engine_Comms.
        /// </summary>
        /// <param name="faction"></param>
        internal void RemoveFaction(Guid faction)
        {
            Messages.Remove(faction);
        }

        /// <summary>
        /// later on add a password parameter to this. 
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public MessageBook RequestMessagebook(Guid faction)
        {
            return Messages[faction];
        }

        public MessageBook FirstOrDefault()
        {
            return Messages.FirstOrDefault().Value;
        }

        /// <summary>
        /// Lib writes messages for the UI  here;
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="faction">faction the message relates to</param>
        /// <param name="message">message object</param>
        internal void LibWriteOutQueue(Guid faction, Message message)
        {
            Messages[faction].OutMessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Lib reads faction messages here. 
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        internal Message LibReadFactionInQueue(Guid faction)
        {
            Message message;  
            Messages[faction].InMessageQueue.TryDequeue(out message);
            return message;
        }

        internal bool LibPeekFactionInQueue(Guid faction, out Message message)
        {
            return Messages[faction].InMessageQueue.TryPeek(out message);
        }

        internal bool LibMessagesWaiting()
        {
            return Messages.Any(book => book.Value.InMessageQueue.Count > 0);
        }

        internal bool LibMessagesWaitingForFaction(Guid faction)
        {
            return Messages[faction].InMessageQueue.Count > 0;
        }
    }

    /// <summary>
    /// A small helper class that wraps the in and out queues relating to a faction.
    /// </summary>
    public class MessageBook
    {
        public Guid Faction { get; private set; }
        public ConcurrentQueue<Message> OutMessageQueue { get; set; }
        public ConcurrentQueue<Message> InMessageQueue { get; set; }

        internal MessageBook(Guid faction)
        {
            OutMessageQueue = new ConcurrentQueue<Message>();
            InMessageQueue = new ConcurrentQueue<Message>();
            Faction = faction;
        }
    }
}
