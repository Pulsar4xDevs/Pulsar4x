using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
namespace WPFUI
{
    class UI_Comms
    {
        Guid factionID;

        Engine_Comms EngineComms { get; set; }
        public UI_Comms(Engine_Comms engineComms)
        {
            EngineComms = engineComms;
        }

        public void CheckEngineMessageQueue(Guid factionID)
        {
            Message message = EngineComms.UIPop(factionID);
            if (message != null)
            {        
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 
            }

        }

        /// <summary>
        /// messages/orders to the game lib.
        /// </summary>
        /// <param name="factionID">for this faction</param>
        /// <param name="message"></param>
        public void SendEngineMessage(Guid factionID, Message message)
        {
            EngineComms.LibPush(factionID, message);
        }

    }

    class UI_Comms2
    {
        MessageBook MessageQueue { get; set; }
        public UI_Comms2(Engine_Comms2 engineComms, Guid factionID)
        {
            MessageQueue = engineComms.RequestMessagebook(factionID);
        }

        public void CheckEnginMessageQueue()
        {
            Message message;
            if (MessageQueue.MessageQueue.TryDequeue(out message))
            {
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 
            }
        }
    }
}
