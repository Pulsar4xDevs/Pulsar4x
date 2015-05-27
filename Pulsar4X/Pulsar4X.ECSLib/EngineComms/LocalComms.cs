using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public class LocalClientTransportLayer : IClientTransportLayer
    {
        public Guid ClientGuid { get; private set; }

        public Queue<Message> MessagesFromLibrary;
        public readonly object LockObj;

        private readonly LocalServerTransportLayer _server;

        /// <summary>
        /// Signals the Transport Layer to stop blocking GetMessage().
        /// </summary>
        public bool Abort { get; set; }

        public LocalClientTransportLayer(Guid clientGuid)
        {
            ClientGuid = clientGuid;
            _server = new LocalServerTransportLayer(this);
            LockObj = new object();
            Abort = false;
        }

        public Message GetMessage()
        {
            while (MessagesFromLibrary.Count == 0 && (!Abort))
            {
                Thread.Sleep(100);
            }
            if (Abort)
            {
                return new Message();
            }
            lock (LockObj)
            {
                return MessagesFromLibrary.Dequeue();
            }
        }

        public bool TryGetMessage(out Message message)
        {
            message = new Message();

            if (MessagesFromLibrary.Count == 0)
            {
                return false;
            }
            lock (LockObj)
            {
                message = MessagesFromLibrary.Dequeue();
                return true;
            }
        }

        public string RequestStatusUpdate(CommandByte status)
        {
            throw new NotImplementedException();
        }

        public BaseDataBlob RequestDataBlob(Guid entityGuid, int typeIndex)
        {
            throw new NotImplementedException();
        }

        public List<Guid> RequestSystemEntities(StarSystem system)
        {
            throw new NotImplementedException();
        }

        public List<StarSystem> RequestStarSystems()
        {
            throw new NotImplementedException();
        }

        public List<Guid> RequestFactions()
        {
            throw new NotImplementedException();
        }

        public void SendCommand(CommandByte command, StatusByte status, string data)
        {
            throw new NotImplementedException();
        }
    }

    public class LocalServerTransportLayer : IServerTransportLayer
    {
        public IClientTransportLayer Client { get { return _client; } }
        private readonly LocalClientTransportLayer _client;
        private readonly LibProcessLayer _common;

        internal readonly Queue<Message> MessagesFromUI;
        internal readonly object LockObj;

        public LocalServerTransportLayer(LocalClientTransportLayer client)
        {
            _client = client;
            MessagesFromUI = new Queue<Message>();
            LockObj = new object();
            _common = new LibProcessLayer();
        }

        public bool ProcessMessage()
        {
            if (MessagesFromUI.Count == 0)
            {
                return false;
            }

            Message currentMessage;
            lock (LockObj)
            {
                currentMessage = MessagesFromUI.Dequeue();
            }

            switch (currentMessage.Type)
            {
                case CommandByte.NewGame:
                    _common.ProcessNewGameMessage(currentMessage);
                    break;
                case CommandByte.SaveGame:
                    _common.ProcessSaveMessage(currentMessage);
                    break;
                case CommandByte.LoadGame:
                    _common.ProcessLoadMessage(currentMessage);
                    break;
                case CommandByte.Pulse:
                    _common.ProcessPulseMessage(currentMessage);
                    break;
                case CommandByte.Quit:
                    _common.ProcessQuitMessage(currentMessage);
                    break;
                case CommandByte.Echo:
                    SendStatusUpdate(CommandByte.Echo, currentMessage.Status, currentMessage.Data.ToString());
                    break;
                default:
                    throw new Exception("Message of type: " + currentMessage.Type + ", Went unprocessed.");
            }

            return (MessagesFromUI.Count > 0);
        }

        public void SendStatusUpdate(CommandByte command, StatusByte status, string data = null)
        {
            throw new NotImplementedException();
        }

        public void SendDataBlob(BaseDataBlob dataBlob)
        {
            throw new NotImplementedException();
        }

        public void SendSystemEntities(StarSystem system)
        {
            throw new NotImplementedException();
        }

        public void SendFactions()
        {
            throw new NotImplementedException();
        }
    }
}
