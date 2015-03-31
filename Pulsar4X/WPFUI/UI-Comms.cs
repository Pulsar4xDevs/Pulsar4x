using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows.Threading;

namespace WPFUI
{

    class UI_Comms
    {
        MessageBook MessageQueue { get; set; }
        public UI_Comms(Engine_Comms engineComms, int factionID)
        {
            MessageQueue = engineComms.RequestMessagebook(factionID);
        }
        
        public void CheckEnginMessageQueue()
        {

            Message message;
            if (MessageQueue.OutMessageQueue.TryDequeue(out message))
            {
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 
                //
                if (MessageQueue.OutMessageQueue.Count > 10)
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => CheckEnginMessageQueue()));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CheckEnginMessageQueue()));
            }
            else
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => CheckEnginMessageQueue()));
        }
    }
}
