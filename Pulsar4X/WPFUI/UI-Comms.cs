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

        public Queue<Message> LocalMessageQueue { get; set; }
        Engine_Comms EngineComms { get; set; }
        public UI_Comms(Engine_Comms engineComms)
        {
            LocalMessageQueue = new Queue<Message>();
            EngineComms = engineComms;
        }

        public void CheckEngineMessageQueue(Guid factionID)
        {
            LocalMessageQueue.Enqueue(EngineComms.UIPop(factionID));
        }

        public void SendEngineMessage(Guid factionID, Message message)
        {
            EngineComms.LibPush(factionID, message);
        }

    }
}
