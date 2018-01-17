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
    public class NetworkClient : NetworkBase, OrderHandler
    {
        struct EntityHashData
        {
            internal Guid EntityID;
            internal DateTime AtDatetime;
            internal int EntityHash;
            internal Dictionary<string, int> DatablobHashes;
        }

        private Dictionary<Guid, EntityCommand> CommandsAwaitingServerAproval = new Dictionary<Guid, EntityCommand>();

        private NetClient NetClientObject { get { return (NetClient)NetPeerObject; } }
        public string HostAddress { get; set; }
        private bool _isConnectedToServer;
        public bool IsConnectedToServer { get { return _isConnectedToServer; } set { _isConnectedToServer = value; OnPropertyChanged(); } }
        private bool _hasFullDataset;
        public bool HasFullDataset { get { return _hasFullDataset; } private set { _hasFullDataset = value; OnPropertyChanged(); } }
        public Entity CurrentFaction { get { return _gameVM.CurrentFaction; } }
        public string ConnectedToGameName { get; private set; }

        //private Dictionary<Guid, string> _factions; 
        //public ObservableCollection<FactionItem> Factions { get; set; }
        private GameVM _gameVM;

        //public Game Game { get; set; }

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


        protected override void PostQueueHandling() //TODO: can maybe get rid of this
        {
            if (CurrentFaction != null)
            {
                //CheckEntityData();
                //_gameVM.StarSystemSelectionViewModel = new StarSystemSelectionVM(_gameVM, Game, CurrentFaction);
                //_gameVM.StarSystemSelectionViewModel.Initialise();
            }

        }

        #region NetMessages

        protected override void HandleDiscoveryResponse(NetIncomingMessage message)
        {
            ConnectedToGameName = message.ReadString();
            long ticks = message.ReadInt64();
            DateTime hostToDatetime = DateTime.FromBinary(ticks); //= new DateTime(ticks);
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

        public void SendSystemDataRequest(Guid systemID)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToServerMsgType.RequestSystemData);
            msg.Write(systemID.ToByteArray());
            NetClientObject.SendMessage(msg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendDatablobRequest(Guid entityID, Type datablobType)
        {
            //TODO: datablobs have an int ID, can we use that? can we be sure that the server and client's datablob IDs are the same?
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToServerMsgType.RequestDatablob);
            msg.Write(entityID.ToByteArray());                          //EntityID
            msg.Write(datablobType.Name);                               //type.
            msg.Write(EntityManager.DataBlobTypes[datablobType]);       //typeIndex

            NetClientObject.SendMessage(msg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendEntityCommand(EntityCommand cmd)
        {
            Guid cmdID = Guid.NewGuid();
            CommandsAwaitingServerAproval.Add(cmdID, cmd);

            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();

            var mStream = new MemoryStream();

            SerializationManager.Export(Game, mStream, cmd); 
            byte[] byteArray = mStream.ToArray();
            int len = byteArray.Length;

            sendMsg.Write((byte)ToServerMsgType.SendPlayerEntityCommand);
            sendMsg.Write(cmdID.ToByteArray());
            sendMsg.Write(cmd.GetType().Name);
            sendMsg.Write(len);
            sendMsg.Write(byteArray);

            NetClientObject.SendMessage(sendMsg, NetClientObject.ServerConnection, NetDeliveryMethod.ReliableOrdered);
            Messages.Add("Sent Command " + cmd.CmdID);
        }

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region HandleIncomingDataMessages

        protected override void HandleIncomingDataMessage(NetConnection sender, NetIncomingMessage message)
        {
            ToClientMsgType messageType = (ToClientMsgType)message.ReadByte();
            switch (messageType)
            {
                case ToClientMsgType.SendTickInfo:
                    HandleTickInfo(message);
                    break;
                case ToClientMsgType.SendGameData:
                    HandleGameSettings(message);
                    break;
                case ToClientMsgType.SendFactionEntity:
                    HandleFactionData(message);
                    break;
                case ToClientMsgType.SendEntityHashData:
                    HandleEntityHashData(message);
                    break;
                case ToClientMsgType.SendEntity:
                    HandleEntityData(message);
                    break;
                case ToClientMsgType.SendSystemData:
                    HandleSystemData(message);
                    break;
                case ToClientMsgType.SendDatablob:
                    HandleDatablob(message);
                    break;
                case ToClientMsgType.SendEntityCommandAck:
                    HandleEntityCommandAproval(message);
                    break;
                case ToClientMsgType.SendStringMessage:
                    HandleStringMessage(message);
                    break;
                case ToClientMsgType.SendEntityChangeData:
                    HandleEntityChangeData(message);
                    break;
                default:
                    throw new Exception("Unhandled ToClientMsgType: " + messageType + " This is probliby caused by a game version difference");
            }
        }

        private void HandleStringMessage(NetIncomingMessage message)
        {
            string messageStr = message.ReadString();
            Messages.Add(messageStr);
        }

        void HandleTickInfo(NetIncomingMessage message)
        {

            DateTime hostToDatetime = DateTime.FromBinary(message.ReadInt64());
            DateTime hostFromDatetime = DateTime.FromBinary(message.ReadInt64());
            TimeSpan hostDelta = hostToDatetime - hostFromDatetime;
            TimeSpan ourDelta = hostToDatetime - Game.GameLoop.GameGlobalDateTime;
            if (ourDelta < TimeSpan.FromSeconds(0))
                throw new Exception("Client has gotten ahead of host, this should not happen");
            string messageStr = "TickEvent: DateTime: " + hostToDatetime + " HostDelta: " + hostDelta + " OurDelta: " + ourDelta;
            Messages.Add(messageStr);
            Game.GameLoop.TimeStep(hostToDatetime);

        }

        void HandleGameSettings(NetIncomingMessage message)
        {
            DateTime hostDateTime = DateTime.FromBinary(message.ReadInt64());
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);


            Game = new Game();
            _gameVM.Game = Game;
            Game.OrderHandler = this;
            var mStream = new MemoryStream(data);
            Game.Settings = SerializationManager.ImportGameSettings(mStream);
            mStream.Close();
            Game.GameLoop.GameGlobalDateTime = hostDateTime;
            //TODO: #CleanCode: the below should probilby be in refactored to somewhere else. 
            //TODO: #Network: it should also maybe be able to request a dataset from the server if it hasn't got it localy. 
            if (Game.Settings.DataSets != null)
            {
                foreach (string dataSet in Game.Settings.DataSets)
                {
                    StaticDataManager.LoadData(dataSet, Game);
                }
            }
            if (Game.StaticData.LoadedDataSets.Count == 0)
            {
                StaticDataManager.LoadData("Pulsar4x", Game);
            }
            mStream.Close();

        }

        void HandleFactionData(NetIncomingMessage message)
        {
            Guid entityID = new Guid(message.ReadBytes(16));
            int hash = message.ReadInt32();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);

            var mStream = new MemoryStream(data);

            bool entityExsists = Game.GlobalManager.EntityExistsGlobaly(entityID);

            Entity factionEntity = SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager);
            mStream.Close();

            if (entityID != factionEntity.Guid)
            {
                Entity otherFactionEntity;
                Game.GlobalManager.FindEntityByGuid(entityID, out otherFactionEntity);
                Messages.Add("Warning! Guid does not match, Something is changing the guid. "); 

            }


            if (hash == factionEntity.GetValueCompareHash())
                Messages.Add("Good news everybody! Faction Hash is a Match!");
            else
            {
                Messages.Add("Warning! Unmatched Faction Hash");
                Messages.Add("OrigionalfactionHash: " + hash.ToString());
                printEntityHashInfo(factionEntity);
            }


            var factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            foreach (var item in factionInfo.KnownSystems)
            {
                SendSystemDataRequest(item);
            }

            _gameVM.CurrentFaction = factionEntity;
            _gameVM.StarSystemSelectionViewModel = new StarSystemSelectionVM(_gameVM, Game, factionEntity);
        }

        void HandleEntityHashData(NetIncomingMessage message)
        {
            Guid entityID = new Guid(message.ReadBytes(16));
            DateTime atDate = new DateTime(message.ReadInt64());
            int entityHash = message.ReadInt32();
            int count = message.ReadInt32();
            Dictionary<string, int> hashDict = new Dictionary<string, int>();

            for (int i = 0; i < count - 1; i++)
            {
                string key = message.ReadString();
                int hash = message.ReadInt32();
                hashDict.Add(key, hash);
            }
            EntityHashData hashData = new EntityHashData
            {
                EntityID = entityID,
                AtDatetime = atDate,
                EntityHash = entityHash,
                DatablobHashes = hashDict
            };
            OnEntityHashDataReceved(hashData);
        }

        void HandleEntityData(NetIncomingMessage message)
        {
            
            Guid entityID = new Guid(message.ReadBytes(16));
            int hash = message.ReadInt32();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            Entity entity = SerializationManager.ImportEntity(Game, mStream, Game.GlobalManager); //I think we need to figure out which manager this is suposed to be going into.
            //TODO: not sure that this is really the best way to do this...
            if (entity.HasDataBlob<PositionDB>())
            {
                var pos = entity.GetDataBlob<PositionDB>();
                var manager = Game.Systems[pos.SystemGuid].SystemManager;
                entity.Transfer(manager);
            }


            mStream.Close();
            if (hash == entity.GetValueCompareHash())
                Messages.Add("Good news everybody! Entity Hash is a Match!");
            else
            {
                
                string name;
                if (entity.HasDataBlob<NameDB>())
                    name = entity.GetDataBlob<NameDB>().DefaultName;
                else
                    name = entity.Guid.ToString();
                Messages.Add("Warning! Unmatched Entity Hash for: " + name);
                Messages.Add("This is likely due to an incorrect IValueCompare implemenation on a datablob");
                Messages.Add("Origional Entity Hash: " + hash.ToString());
                printEntityHashInfo(entity);
            }

            if (entity.HasDataBlob<OwnedDB>())
            {
                var owned = entity.GetDataBlob<OwnedDB>();
                var owner = owned.OwnedByFaction.GetDataBlob<OwnerDB>();
                if (!owner.OwnedEntities.ContainsKey(entity.Guid))
                {
                    Messages.Add("owned entity not being set to owner, setting now");
                    owner.OwnedEntities[entity.Guid] = entity;
                }
                else if (owner.OwnedEntities[entity.Guid] != entity)
                {
                    Messages.Add("Error: Owner has entity, but does not match");
                }
                else 
                {
                    Messages.Add("ownership seems to have added ok");
                }
            }


        }

        void HandleSystemData(NetIncomingMessage message)
        {

            Guid systemID = new Guid(message.ReadBytes(16));
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            StarSystem starSys = SerializationManager.ImportSystem(Game, mStream);
            mStream.Close();

            Messages.Add("Recevied StarSystem: " + starSys.NameDB.DefaultName);
            _gameVM.StarSystemSelectionViewModel.StarSystems.Add(starSys, starSys.NameDB.GetName(this.CurrentFaction));
            starSys.SystemManager.ManagerSubpulses.Initalise(starSys.SystemManager);

        }

        void HandleDatablob(NetIncomingMessage message)
        {
            Guid entityGuid = new Guid(message.ReadBytes(16));
            string name = message.ReadString();
            int typeIndex = message.ReadInt32();
            //int hash = message.ReadInt32();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);

            var mStream = new MemoryStream(data);
            Entity entity;
            if (!this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                Messages.Add("DatablobImportFail: No Entity for Guid: " + entityGuid);
            else
            {
                string fullName = "Pulsar4X.ECSLib." + name; 
                Type dbType = Type.GetType(fullName);
                if (dbType == null)
                    throw new Exception("DataBlob "+ fullName +" not found. " +
                                        "Either the client game version does not match the server, " +
                                        "or the datablob is not in the expected namespace. " +
                                        "This section of code does not support datablobs not in the Pulsar4X.ECSLib namespace, " +
                                        "either fix this code or ensure the datablob is in the appropriate namespace");
                BaseDataBlob db = SerializationManager.ImportDatablob(Game, entity, dbType, mStream);
            }
        }

        void HandleEntityCommandAproval(NetIncomingMessage message)
        {
            Guid cmdID = new Guid(message.ReadBytes(16));
            bool valid = message.ReadBoolean();
            EntityCommand cmd = CommandsAwaitingServerAproval[cmdID];
            if (valid)
            {
                cmd.EntityCommanding.GetDataBlob<OrderableDB>().ActionList.Add(cmd);
                var commandList = cmd.EntityCommanding.GetDataBlob<OrderableDB>().ActionList;
                OrderableProcessor.ProcessOrderList(Game, commandList);
            }
            else
            {
                Messages.Add("Command not validated by server"); //TODO: maybe add messages from server saying why. 
            }
            CommandsAwaitingServerAproval.Remove(cmdID);
        }

        void HandleEntityChangeData(NetIncomingMessage message)
        {
            EntityChangeData.EntityChangeType changeType = (EntityChangeData.EntityChangeType)message.ReadByte();
            switch (changeType)
            {
                case EntityChangeData.EntityChangeType.EntityAdded:
                    {
                        HandleEntityData(message);
                    }
                    break;
                case EntityChangeData.EntityChangeType.EntityRemoved:
                    {
                        Guid entityID = new Guid(message.ReadBytes(16));
                        Entity entity;
                        if (!this.Game.GlobalManager.FindEntityByGuid(entityID, out entity))
                            Messages.Add("Remove Entity Fail: No Entity for Guid: " + entityID);
                        else
                            entity.Manager.RemoveEntity(entity);
                    }
                    break;
                case EntityChangeData.EntityChangeType.DBAdded:
                    {
                        HandleDatablob(message);
                    }
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    {
                        Guid entityID = new Guid(message.ReadBytes(16));    //entity guid. 
                        string name = message.ReadString();                 //datablob name
                        int typeIndex = message.ReadInt32();                //datablob typeIndex
                        Entity entity;
                        if (!this.Game.GlobalManager.FindEntityByGuid(entityID, out entity))
                            Messages.Add("Remove " + name + " Datablob Fail: No Entity for Guid: " + entityID + " this is not critical and can be ignored.");
                        else
                        {
                            if (entity.HasDataBlob(typeIndex))
                            {
                                entity.RemoveDataBlob(typeIndex);
                                Messages.Add("Successfully removed " + name + " Datablob from entity " + entityID);
                            }
                            else
                                Messages.Add("Remove " + name + " Datablob Fail: datablob not found in entity, this is not critical and can be ignored.");

                        }
                    }
                    break;
                default:
                    throw new Exception("Network classes need to handle EntityChangeType, this is probibly caused by a game version missmatch");
            }
        }

        #endregion

        #region Other





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

        /*
        void SynchGameWithHost(Game game, Entity faction)
        {
            //maybe this info should be pushed from the server. 
            throw new NotImplementedException();
            var managers = game.GlobalManagerDictionary.Keys;    //might need to change how managers and the serialiser works for this. 
            //foreach manager in game
                //foreach entity owned by faction in manager
                    //check hash
                        //check datablobhash
        }
        */

        void SynchFactionWithHost(Game game, Entity faction)
        {
            

            //TODO: subscribe to factionhash as a listner?, need to write some events. 
            SendFactionHashRequest(faction.Guid);

            int hostFactionHash = 0;
            Dictionary<string, int> factiondbHashes = new Dictionary<string, int>();

            int localFactionHash = faction.GetValueCompareHash();

            if (hostFactionHash != localFactionHash)
            {
                foreach (IGetValuesHash db in faction.DataBlobs)
                {
                    string dbname = db.GetType().ToString();
                    if(factiondbHashes.ContainsKey(dbname))
                    {
                        if (factiondbHashes[dbname] != db.GetValueCompareHash())
                        { }
                    }
                }
                //foreach system in known systems
                //  foreach entity in system
                        //check entityhash
                            //check datablob hashes
            }
            throw new NotImplementedException();
        }

        void OnEntityHashDataReceved(EntityHashData hashData)
        {
            Entity entity;
            if (!_gameVM.Game.GlobalManager.FindEntityByGuid(hashData.EntityID, out entity))
            {
                //not sure this should even happen... but just incase
                SendEntityDataRequest(hashData.EntityID);
            }
            else 
            {
                if (entity.Manager.ManagerSubpulses.SystemLocalDateTime != hashData.AtDatetime)
                { Messages.Add("Unmatched Date, can't compare hashes"); }
                else
                {
                    if (entity.GetHashCode() != hashData.EntityHash)
                    {
                        foreach (var datablob in entity.DataBlobs)
                        {
                            if (!hashData.DatablobHashes.ContainsKey(datablob.GetType().ToString()))
                                entity.RemoveDataBlob(EntityManager.DataBlobTypes[datablob.GetType()]);
                            if (datablob.GetHashCode() != hashData.DatablobHashes[datablob.GetType().ToString()])
                            {
                                //SendDatablobRequest(hashData.EntityID, datablob.GetType());
                                Messages.Add("Datablob hashes don't match: " + datablob.GetType());

                            }
                        }
                    }
                }
            }
        }

        public void HandleOrder(EntityCommand entityCommand) //TODO: maybe rename this since in the netclient and server 'HandleFoo' tends to represent handling a network message.  (this is from the OrderHandler interface)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            SendEntityCommand(entityCommand);
        }

        #endregion
    }
}
