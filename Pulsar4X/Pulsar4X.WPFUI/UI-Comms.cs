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

        private Thread _mainLoopThread;
        private MessageBook _messageQueue;

        public UIComms(EngineComms engineComms, Entity faction)
        {
            Instance = this;
            _messageQueue = engineComms.RequestMessagebook(faction);
            GlobalManager = new EntityManager();
        }
        
        public void CheckEngineMessageQueue()
        {
            Message message;
            if (_messageQueue.OutMessageQueue.TryDequeue(out message))
            {
                //what you would need is a message processor function, this function would look at the message type and then fire of an event for that message type.
                //everthing in the UI would subscribe to the events for messages they are interested in
                //the even would pass the data, i.e the rest of the message object, to the event delegates 

                switch(message.Type)
                {
                    case (Message.MessageType.GameStatusUpdate):
                        break;
                }

                if (_messageQueue.OutMessageQueue.Count > 10)
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => CheckEngineMessageQueue()));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CheckEngineMessageQueue()));
            }
            else
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => CheckEngineMessageQueue()));
        }

        #region Communications

        public void SendMessage(Message message, bool forceEngineFire = true)
        {
            if(forceEngineFire && !IsEngineFired())
                FireEngine();
            _messageQueue.InMessageQueue.Enqueue(message);
        }

        public void FireEngine()
        {
            if(IsEngineFired())
                return;
            _mainLoopThread = new Thread(Game.Instance.MainGameLoop);//Replace it with something better later
            _mainLoopThread.Start();
        }

        public void HaltEngine()
        {
            if(!IsEngineFired())
                return;
            if (_mainLoopThread != null)
            {
                if (_mainLoopThread.ThreadState == ThreadState.Stopped) //we can safely abort it to make sure everything is ok
                    _mainLoopThread.Abort();
                else
                {
                    SendMessage(new Message(Message.MessageType.Quit, null));
                    Thread.Sleep(100);
                    if (_mainLoopThread.ThreadState == ThreadState.Stopped)
                        _mainLoopThread.Abort();
                    else
                        throw new Exception("Game Loop can't be stopped");
                }
            }
        }

        public bool IsEngineFired()
        {
            if(_mainLoopThread == null)
                return false;
            if(_mainLoopThread.ThreadState == ThreadState.Stopped)
                return false;
            return true;
        }

        #endregion

        #region Data

        public EntityManager GlobalManager { get; private set; }

        #endregion

        #region Events

        #endregion
    }
}
