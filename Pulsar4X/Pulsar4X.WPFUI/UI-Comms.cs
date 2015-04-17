using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows.Threading;

namespace Pulsar4X.WPFUI
{
    internal class UI_Comms
    {
        internal static UI_Comms Instance { get; set; }
        internal static Thread MainLoopThread { get; private set; }

        MessageBook MessageQueue { get; set; }

        public UI_Comms(Engine_Comms engineComms, Entity faction)
        {
            MessageQueue = engineComms.RequestMessagebook(faction);
            FireEngine();
        }
        
        public void CheckEngineMessageQueue()
        {
            Message message;
            if (MessageQueue.OutMessageQueue.TryDequeue(out message))
            {
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 

                if (MessageQueue.OutMessageQueue.Count > 10)
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => CheckEngineMessageQueue()));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CheckEngineMessageQueue()));
            }
            else
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => CheckEngineMessageQueue()));
        }

        private void FireEngine()
        {
            if(MainLoopThread != null)
                MainLoopThread.Abort(); //Could be cause of problems
            MainLoopThread = new Thread(Game.Instance.MainGameLoop);//Replace it with something better later
            MainLoopThread.Start();
        }
    }
}
