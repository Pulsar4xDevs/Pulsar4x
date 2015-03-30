using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Engine side of the Lib-UI communications. 
    /// engine needs to create one of these when instantiated, and then AddFactions as factions are added to the game.
    /// </summary>
    public class Engine_Comms
    {
        Dictionary<int, MessageBook> Messages = new Dictionary<int, MessageBook>();

        /// <summary>
        /// a dictionary of faction names and thier guids. mostly so the ui can request the correct factionid for a given faction name. 
        /// </summary>
        public Dictionary<string, int> Factions = new Dictionary<string, int>();
        
        /// <summary>
        /// Engine_comms constructor. 
        /// </summary>
        internal Engine_Comms()
        { }

        public void AddFaction(int factionID)
        {
            Messages.Add(factionID, new MessageBook(factionID));
            //Factions.Add(FactionEntity.Name, factionID)
        }

        /// <summary>
        /// if a faction is killed off or otherwise removed from the game it needs to be removed from Engine_Comms.
        /// </summary>
        /// <param name="factionID"></param>
        internal void RemoveFaction(int factionID)
        {
            Messages.Remove(factionID);
            //Factions.Remove()
        }

        /// <summary>
        /// later on add a password parameter to this. 
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
        public MessageBook RequestMessagebook(int factionID)
        {
            return Messages[factionID];
        }

        /// <summary>
        /// lib writes messages for the UI  here;
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="factionID">faction the message relates to</param>
        /// <param name="message">message object</param>
        internal void LibWriteOutQueue(int factionID, Message message)
        {
            Messages[factionID].OutMessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Lib reads faction messages here. 
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
        internal Message LibReadFactionInQueue(int factionID)
        {
            Message message;  
            Messages[factionID].InMessageQueue.TryDequeue(out message);
            return message;
        }

        internal bool LibPeekFactionInQueue(int factionID, out Message message)
        {
            return Messages[factionID].InMessageQueue.TryPeek(out message);
        }

        internal bool LibMessagesWaiting()
        {
            foreach(var book in Messages)
            {
                if (book.Value.InMessageQueue.Count > 0)
                    return true;
            }

            return false;
        }

        internal bool LibMessagesWaitingForFaction(int factionID)
        {
            if (Messages[factionID].InMessageQueue.Count > 0)
                return true;

            return false;
        }
    }

    /// <summary>
    /// this needs to be fleshed out.
    /// </summary>
    public class Message
    {
        public enum MessageType
        {
            Quit,           ///< terminates the main game loop.
            Echo,           ///< will be sent straight back to sender. Use for testing.     
        }

        public MessageType _messageType;
        public object _data;

        public Message(MessageType type, object data)
        {
            _messageType = type;
            _data = data;
        }

        public Message() { }
    }


    /// <summary>
    /// 
    /// </summary>
    public class MessageBook
    {
        int _FactionID;
        public ConcurrentQueue<Message> OutMessageQueue { get; set; }
        public ConcurrentQueue<Message> InMessageQueue { get; set; }
        internal MessageBook(int factionID)
        {
            OutMessageQueue = new ConcurrentQueue<Message>();
            InMessageQueue = new ConcurrentQueue<Message>();
            _FactionID = factionID;
        }

        public int FactionID
        {
            get { return _FactionID;  }
        }
    }
}
