﻿using System;
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
        Dictionary<Entity, MessageBook> Messages = new Dictionary<Entity, MessageBook>();

        /// <summary>
        /// a dictionary of faction names and thier guids. mostly so the ui can request the correct factionid for a given faction name. 
        /// </summary>
        public Dictionary<string, Entity> Factions = new Dictionary<string, Entity>();
        
        /// <summary>
        /// Engine_comms constructor. 
        /// </summary>
        internal Engine_Comms()
        { }

        public void AddFaction(Entity faction)
        {
            Messages.Add(faction, new MessageBook(faction));
            //Factions.Add(FactionEntity.Name, factionID)
        }

        /// <summary>
        /// if a faction is killed off or otherwise removed from the game it needs to be removed from Engine_Comms.
        /// </summary>
        /// <param name="faction"></param>
        internal void RemoveFaction(Entity faction)
        {
            Messages.Remove(faction);
            //Factions.Remove()
        }

        /// <summary>
        /// later on add a password parameter to this. 
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public MessageBook RequestMessagebook(Entity faction)
        {
            return Messages[faction];
        }

        public MessageBook FirstOrDefault()
        {
            return Messages.FirstOrDefault().Value;
        }

        /// <summary>
        /// lib writes messages for the UI  here;
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="faction">faction the message relates to</param>
        /// <param name="message">message object</param>
        internal void LibWriteOutQueue(Entity faction, Message message)
        {
            Messages[faction].OutMessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Lib reads faction messages here. 
        /// or can just get the messagebook via Messages[factionID]. 
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        internal Message LibReadFactionInQueue(Entity faction)
        {
            Message message;  
            Messages[faction].InMessageQueue.TryDequeue(out message);
            return message;
        }

        internal bool LibPeekFactionInQueue(Entity faction, out Message message)
        {
            return Messages[faction].InMessageQueue.TryPeek(out message);
        }

        internal bool LibMessagesWaiting()
        {
            return Messages.Any(book => book.Value.InMessageQueue.Count > 0);
        }

        internal bool LibMessagesWaitingForFaction(Entity faction)
        {
            return Messages[faction].InMessageQueue.Count > 0;
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
        public Entity Faction { get; private set; }
        public ConcurrentQueue<Message> OutMessageQueue { get; set; }
        public ConcurrentQueue<Message> InMessageQueue { get; set; }
        internal MessageBook(Entity faction)
        {
            OutMessageQueue = new ConcurrentQueue<Message>();
            InMessageQueue = new ConcurrentQueue<Message>();
            Faction = faction;
        }
    }
}
