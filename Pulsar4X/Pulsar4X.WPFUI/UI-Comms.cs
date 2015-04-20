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
    internal delegate void StatusUpdate(string message);
    internal class UIComms
    {
        internal static UIComms Instance { get; private set; }

        #region Events

        internal StatusUpdate OnStatusUpdate;

        #endregion
        
        internal Thread MainLoopThread { get; private set; }

        private MessageBook MessageQueue { get; set; }

        public UIComms(EngineComms engineComms, Entity faction)
        {
            Instance = this;
            MessageQueue = engineComms.RequestMessagebook(faction);
        }
        
        public void CheckEngineMessageQueue()
        {
            Message message;
            if (MessageQueue.OutMessageQueue.TryDequeue(out message))
            {
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 

                switch(message.Type)
                {
                    case (Message.MessageType.GameStatusUpdate):
                        if(OnStatusUpdate != null)
                            OnStatusUpdate.Invoke((string)message.Data);
                        break;
                }

                if (MessageQueue.OutMessageQueue.Count > 10)
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => CheckEngineMessageQueue()));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CheckEngineMessageQueue()));
            }
            else
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => CheckEngineMessageQueue()));
        }

        public void SendMessage(Message message, bool forceEngineFire = true)
        {
            if(forceEngineFire && !IsEngineFired())
                FireEngine();
            MessageQueue.InMessageQueue.Enqueue(message);
        }

        public void FireEngine()
        {
            if(IsEngineFired())
                return;
            MainLoopThread = new Thread(Game.Instance.MainGameLoop);//Replace it with something better later
            MainLoopThread.Start();
        }

        public void HaltEngine()
        {
            if(!IsEngineFired())
                return;
            if (MainLoopThread != null)
            {
                if (MainLoopThread.ThreadState == ThreadState.Stopped) //we can safely abort it to make sure everything is ok
                    MainLoopThread.Abort();
                else
                {
                    SendMessage(new Message(Message.MessageType.Quit, null));
                    Thread.Sleep(100);
                    if (MainLoopThread.ThreadState == ThreadState.Stopped)
                        MainLoopThread.Abort();
                    else
                        throw new Exception("Game Loop can't be stopped");
                }
            }
        }

        public bool IsEngineFired()
        {
            if(MainLoopThread == null)
                return false;
            if(MainLoopThread.ThreadState == ThreadState.Stopped)
                return false;
            return true;
        }
    }
}
