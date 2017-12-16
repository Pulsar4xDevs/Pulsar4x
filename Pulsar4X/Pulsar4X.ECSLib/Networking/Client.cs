using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Threading;
using Lidgren.Network;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Networking
{
    enum ToServerMsgType : byte
    {
        RequestSystemData,
        RequestFactionEntityData,
        RequestFactionEntityHash,
        RequestEntityData,
        RequestEntityHash,
        RequestDatablob,
        RequestDatablobHash,
        SendPlayerEntityCommand
    }
    public class NetworkClient : NetworkBase
    {

        private NetClient NetClientObject { get { return (NetClient)NetPeerObject; } }
        public string HostAddress { get; set; }
        private bool _isConnectedToServer;
        public bool IsConnectedToServer { get { return _isConnectedToServer; } set { _isConnectedToServer = value; OnPropertyChanged(); } }
        private bool _hasFullDataset;
        public bool HasFullDataset { get { return _hasFullDataset; } private set { _hasFullDataset = value; OnPropertyChanged(); } }
        public Entity CurrentFaction { get; set; }
        //public event TickStartEventHandler NetTickEvent;
        public string ConnectedToGameName { get; private set; }
        public DateTime hostToDatetime { get; private set; }
        //private Dictionary<Guid, string> _factions; 
        //public ObservableCollection<FactionItem> Factions { get; set; }
        private GameVM _gameVM;

        public NetworkClient(string hostAddress, int portNum, GameVM gameVM)
        {
            _gameVM = gameVM;
            PortNum = portNum;
            HostAddress = hostAddress;
            IsConnectedToServer = false;
            //Factions = new ObservableCollection<FactionItem>();

            var config = new NetPeerConfiguration("Pulsar4X");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            NetPeerObject = new NetClient(config);
        }

        public void ClientConnect()
        {

            NetPeerObject.Start();
            NetClientObject.DiscoverLocalPeers(PortNum);
            NetPeerObject.Connect(host: HostAddress, port: PortNum);
            StartListning();

        }


        protected override void PostQueueHandling()
        {
            if (CurrentFaction != null)
            {
                CheckEntityData();
            }
        }

        #region NetMessages

        protected override void HandleDiscoveryResponse(NetIncomingMessage message)
        {
            ConnectedToGameName = message.ReadString();
            long ticks = message.ReadInt64();
            hostToDatetime = DateTime.FromBinary(ticks); //= new DateTime(ticks);
            Messages.Add("Found Server: " + message.SenderEndPoint + "Name Is: " + ConnectedToGameName);
        }




        protected override void ConnectionStatusChanged(NetIncomingMessage message)
        {
            switch (message.SenderConnection.Status)
            {
                case NetConnectionStatus.Connected:
                    IsConnectedToServer = true;
                    break;
                case NetConnectionStatus.Disconnected:
                    IsConnectedToServer = false;
                    HasFullDataset = false;
                    break;
            }
        }

        #endregion


        #region SendDataMessages

        /// <summary>
        /// Sends the faction data request. and sets up a link between this connection and a faction.
        /// </summary>
        /// <param name="factionName">Faction.</param>
        /// <param name="password">Password.</param>
        public void SendFactionDataRequest(string factionName, string password)
        {
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToServerMsgType.RequestFactionEntityData);
            sendMsg.Write(factionName);
            sendMsg.Write(password);
            Encrypt(sendMsg);//sequence channel 31 is expected to be encrypted by the recever. see NetworkBase GotMessage()
            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered, SecureChannel);
        }

        public void SendFactionHashRequest(Guid factionGuid)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToServerMsgType.RequestFactionEntityHash);
            msg.Write(factionGuid.ToByteArray());
            NetClientObject.SendMessage(msg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendEntityDataRequest(Guid guid)
        {
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToServerMsgType.RequestEntityData);
            sendMsg.Write(guid.ToByteArray());
            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendEntityHashRequest(Guid entityGuid)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToServerMsgType.RequestEntityHash);
            msg.Write(entityGuid.ToByteArray());
            NetClientObject.SendMessage(msg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendEntityCommand(EntityCommand cmd)//may need to serialise it prior to this (as the actual class). can we serialise an interface?
        {
            throw new NotImplementedException("Need to figure out how we're going to serialise the EntityCommand object");
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();

            var mStream = new MemoryStream();

            //TODO: SerializationManager.???(cmd, mStream); or figure out exactly how we're going to serialise this, migth have to write a serialiser to handle interface.
            byte[] byteArray = mStream.ToArray();

            int len = byteArray.Length;

            sendMsg.Write((byte)ToServerMsgType.SendPlayerEntityCommand);

            sendMsg.Write(len);
            sendMsg.Write(byteArray);

            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        #endregion





        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void HandleIncomingDataMessage(NetConnection sender, NetIncomingMessage message)
        {
            ToClientMsgType messageType = (ToClientMsgType)message.ReadByte();
            switch (messageType)
            {
                case ToClientMsgType.TickInfo:
                    HandleTickInfo(message);
                    break;
                case ToClientMsgType.SendGameData:
                    HandleGameSettingsMsg(message);
                    break;

                case ToClientMsgType.SendEntityHashData:
                    HandleTickInfo(message);
                    break;
                case ToClientMsgType.SendSystemData:
                    HandleSystemData(message);
                    break;
            }
        }

        #region HandleIncomingDataMessages

        void HandleTickInfo(NetIncomingMessage message)
        {

            hostToDatetime = DateTime.FromBinary(message.ReadInt64());
            DateTime hostFromDatetime = DateTime.FromBinary(message.ReadInt64());
            TimeSpan hostDelta = hostToDatetime - hostFromDatetime;
            TimeSpan ourDelta = hostToDatetime - Game.GameLoop.GameGlobalDateTime;
            if (ourDelta < TimeSpan.FromSeconds(0))
                throw new Exception("Client has gotten ahead of host, this should not happen");
            string messageStr = "TickEvent: DateTime: " + hostToDatetime + " HostDelta: " + hostDelta + " OurDelta: " + ourDelta;
            Messages.Add(messageStr);
            Game.GameLoop.TimeStep(hostToDatetime);

        }

        void HandleGameSettingsMsg(NetIncomingMessage message)
        {

            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            Game = new Game();
            _gameVM.Game = Game;
            var mStream = new MemoryStream(data);
            Game.Settings = SerializationManager.ImportGameSettings(mStream);
            mStream.Close();
        }


        /* Just use HandleEntityData
        void HandleFactionData(NetIncomingMessage message)
        {


            Guid entityID = new Guid(message.ReadBytes(16));
            int hash = message.ReadInt32();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            Entity entity = SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager);
            Messages.Add("OrigionalfactionHash: " + hash.ToString());
            printEntityHashInfo(entity);

        }*/


        void HandleEntityData(NetIncomingMessage message)
        {
            
            Guid entityID = new Guid(message.ReadBytes(16));
            int hash = message.ReadInt32();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            Entity entity = SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager);
            Messages.Add("OrigionalEntityHash: " + hash.ToString());
            printEntityHashInfo(entity);

        }

        void HandleEntityHashData(NetIncomingMessage message)
        {   
            //this is going to need a datetime. 
            int count = message.ReadInt32();
            Dictionary<string, int> hashDict = new Dictionary<string, int>();
            for (int i = 0; i < count - 1; i++)
            {
                string key = message.ReadString();
                int hash = message.ReadInt32();
                hashDict.Add(key, hash);
            }
        }



        void HandleSystemData(NetIncomingMessage message)
        {

            Guid systemID = new Guid(message.ReadBytes(16));
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            //string data = message.ReadString();
            var mStream = new MemoryStream(data);
            StarSystem starSys = SerializationManager.ImportSystem(Game, mStream);

        }

        #endregion

        #region Other

        /// <summary>
        /// checks for entitys which have a guid but contain no info, and requests that data. Obsolete?
        /// </summary>
        private void CheckEntityData()
        {

            int emptyEntities = 0;
            foreach (var entity in Game.GlobalManager.Entities)
            {
                if (entity != null && entity.DataBlobs.Count == 0)
                {
                    emptyEntities++;
                    SendEntityDataRequest(entity.Guid);
                }
            }
            foreach (var system in Game.Systems.Values)
            {
                foreach (var entity in system.SystemManager.Entities)
                {
                    if (entity != null && entity.DataBlobs.Count == 0)
                    {
                        emptyEntities++;
                        SendEntityDataRequest(entity.Guid);
                    }
                }
            }
            if (emptyEntities == 0 && !HasFullDataset)
            {
                HasFullDataset = true;
            }

        }

        #endregion
    }
}
