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



        protected override void HandleDiscoveryResponse(NetIncomingMessage message)
        {
            ConnectedToGameName = message.ReadString();
            long ticks = message.ReadInt64();
            hostToDatetime = DateTime.FromBinary(ticks); //= new DateTime(ticks);
            Messages.Add("Found Server: " + message.SenderEndPoint + "Name Is: " + ConnectedToGameName);
        }

        protected override void HandleGameDataMessage(NetIncomingMessage message)
        {

            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            Game = new Game();
            _gameVM.Game = Game;
            var mStream = new MemoryStream(data);
            Game.Settings = SerializationManager.RXNetStreamGameSettings(mStream);
            mStream.Close();
        }



        protected override void HandleTickInfo(NetIncomingMessage message)
        {
            
            hostToDatetime = DateTime.FromBinary(message.ReadInt64());
            DateTime hostFromDatetime = DateTime.FromBinary(message.ReadInt64());
            TimeSpan hostDelta = hostToDatetime - hostFromDatetime;
            TimeSpan ourDelta = hostToDatetime - Game.GameLoop.GameGlobalDateTime;
            if (ourDelta < TimeSpan.FromSeconds(0))
                throw new Exception("Client has gotten ahead of host, this should not happen");
            string messageStr = "TickEvent: DateTime: " + hostToDatetime + " HostDelta: " + hostDelta + " OurDelta: " + ourDelta;
            Messages.Add(messageStr);
            //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Messages.Add((messageStr))));

            Game.GameLoop.TimeStep(hostToDatetime);

        }

        protected override void HandleFactionData(NetIncomingMessage message)
        {


            Guid entityID = new Guid(message.ReadBytes(16));
            //HandleEntityData(message);

            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            Entity factionEntity = SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager);
            //Guid entityID = factionEntity.Guid;
            //if (Game.GlobalManager.TryGetEntityByGuid(entityID, out factionEntity))
            CurrentFaction = factionEntity;
            _gameVM.CurrentFaction = factionEntity;
        }

        protected override void HandleSystemData(NetIncomingMessage message)
        {
            
            Guid systemID = new Guid(message.ReadBytes(16));
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            //string data = message.ReadString();
            var mStream = new MemoryStream(data);
            //SerializationManager.ImportSystem(Game, mStream);
            StarSystem starSys = SerializationManager.ImportSystem(Game, mStream);    

        }

        protected override void HandleEntityData(NetIncomingMessage message)
        {
            
            Guid entityID = new Guid(message.ReadBytes(16));
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);

            /*
            if (Game == null || Game.GameName != ConnectedToGameName) //TODO handle if connecting to a game when in the middle of a singleplayer game. (ie prompt save)
            {
                Game = Game.NewGame(ConnectedToGameName, ConnectedToDateTime, 0, null, false);                
            }
            */
            var mStream = new MemoryStream(data);

            SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager); //wait, global manager? do I need to serialise the manager ID first? no that can't be right, how does file saving/loading work then? poor commenting on the seralisation manager.

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


        /// <summary>
        /// Sends the faction data request. and sets up a link between this connection and a faction.
        /// </summary>
        /// <param name="factionName">Faction.</param>
        /// <param name="password">Password.</param>
        public void SendFactionDataRequest(string factionName, string password)
        {
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)DataMessageType.FactionData);
            sendMsg.Write(factionName);
            sendMsg.Write(password);
            Encrypt(sendMsg);//sequence channel 31 is expected to be encrypted by the recever. see NetworkBase GotMessage()
            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered, SecureChannel);
        }

        public void SendEntityDataRequest(Guid guid)
        {
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)DataMessageType.EntityData);
            sendMsg.Write(guid.ToByteArray());
            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendEntityCommand(EntityCommand cmd)
        {            
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)DataMessageType.EntityCommand);
            //sendMsg.Write(cmd); //TODO: seralise cmd and write it to the message.
            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }



        /// <summary>
        /// checks for entitys which have a guid but contain no info, and requests that data.
        /// </summary>
        private void CheckEntityData()
        {
            
            int emptyEntities = 0;
            foreach (var entity in Game.GlobalManager.Entities)
            {
                if (entity != null && entity.DataBlobs.Count == 0)
                {
                    emptyEntities ++;
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

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
