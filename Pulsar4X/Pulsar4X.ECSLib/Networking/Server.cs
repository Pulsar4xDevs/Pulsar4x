using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows.Threading;
using Lidgren.Network;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Networking
{
    enum ToClientMsgType : byte
    {
        TickInfo,
        SendGameData,
        SendEntity,
        SendEntityHashData,
        SendDatablob,
        SendSystemData,
        SendFactionEntity
    }

    public class NetworkHost : NetworkBase
    {

        private Dictionary<NetConnection, Guid> _connectedFactions { get; } = new Dictionary<NetConnection, Guid>();
        private Dictionary<Guid, List<NetConnection>> _factionConnections { get; } = new Dictionary<Guid, List<NetConnection>>();
        public NetServer NetServerObject { get { return (NetServer)NetPeerObject; } }

        private DateTime _currentDate;



        public NetworkHost(Game game, int portNum)
        {
            PortNum = portNum;
            Game = game;
            NetHostStart();
        }

        /// <summary>
        /// Adds the faction netcon link if it's not already there. 
        /// </summary>
        /// <param name="faction">Faction.</param>
        /// <param name="netCon">Net con.</param>
        private void AddFactionNetconLink(Entity faction, NetConnection netCon)
        {
            if (!_connectedFactions.ContainsKey(netCon))
                _connectedFactions.Add(netCon, faction.Guid);
            else
                _connectedFactions[netCon] = faction.Guid;
            if (!_factionConnections.ContainsKey(faction.Guid))
            {
                _factionConnections.Add(faction.Guid, new List<NetConnection>());
            }
            if (!_factionConnections[faction.Guid].Contains(netCon))
                _factionConnections[faction.Guid].Add(netCon);

        }

        /// <summary>
        /// Removes the faction netcon link if it exsists.
        /// (will check that the netcon is linked to a faction)
        /// </summary>
        /// <param name="netCon">Net con.</param>
        private void RemoveFactionNetconLink(NetConnection netCon)
        {
            if (_connectedFactions.ContainsKey(netCon))
            {
                Guid factionGuid = _connectedFactions[netCon];
                _connectedFactions.Remove(netCon);
                //if (_factionConnections.ContainsKey(factionGuid))
                //{if (_factionConnections[factionGuid].Contains(netCon)) //these checks should be unnecisary. 
                _factionConnections[factionGuid].Remove(netCon);
            }
        }

        private void NetHostStart()
        {
            var config = new NetPeerConfiguration("Pulsar4X") { Port = PortNum };
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);

            NetPeerObject = new NetServer(config);
            try
            {
                NetPeerObject.Start();
            }
            catch (System.Net.Sockets.SocketException)
            { }
            //EntityDataSanitiser.Initialise(Game);
            StartListning();
            Game.GameLoop.GameGlobalDateChangedEvent += SendTickInfo;
        }






        protected override void HandleDiscoveryRequest(NetIncomingMessage message)
        {

            Messages.Add("RX DiscoveryRequest " + message.SenderEndPoint);
            NetOutgoingMessage response = NetServerObject.CreateMessage();
            response.Write("Pulsar4x Game");//Game.GameName);
            response.Write(Game.CurrentDateTime.ToBinary());

            NetServerObject.SendDiscoveryResponse(response, message.SenderEndPoint);

        }

        protected override void ConnectionStatusChanged(NetIncomingMessage message)
        {
            switch (message.SenderConnection.Status)
            {
                case NetConnectionStatus.Connected:
                    break;
                case NetConnectionStatus.Disconnected:
                    RemoveFactionNetconLink(message.SenderConnection);
                    break;
            }
        }


        #region Handle Incoming Data Messages

        protected override void HandleIncomingDataMessage(NetConnection sender, NetIncomingMessage message)
        {
            ToServerMsgType messageType = (ToServerMsgType)message.ReadByte();
            switch (messageType)
            {

                case ToServerMsgType.RequestFactionEntityData:
                    HandleRequestFactionData(message);
                    break;
                case ToServerMsgType.RequestFactionEntityHash:
                    HandleRequestFactionData(message);
                    break;
                case ToServerMsgType.RequestSystemData:
                    HandleRequestSystemData(message);
                    break;
                case ToServerMsgType.RequestEntityData:
                    HandleRequestEntityData(message);
                    break;
                case ToServerMsgType.RequestEntityHash:
                    HandleRequestEntityHash(message);
                    break;
                case ToServerMsgType.RequestDatablob:
                    HandleRequestDatablob(message);
                    break;
                default:
                    throw new Exception("Unhandled ToServerMsgType: " + messageType);
            }
        }


        void HandleRequestFactionData(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            string name = message.ReadString();
            string pass = message.ReadString();
            List<Entity> factions = Game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();

            //TODO send a message instead of crashing with an exception if we can't find the faction.
            Entity faction = factions.Find(item => item.GetDataBlob<NameDB>().DefaultName == name);

            if (AuthProcessor.Validate(faction, pass))
            {
                AddFactionNetconLink(faction, sender);
                SendGameSettings(sender, Game);
                //TODO negotiate and get a delta between the server's data and the client's data, and only update teh client with changed data.
                //(the client should be able to save/keep a cashe of it's own data so there's less to send when reconnecting to a server.)
                SendFactionData(sender, faction);
                Messages.Add("Sent Faction " + name);
                //printEntityHashInfo(faction);
            }
        }

        void HandleRequestSystemData(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            StarSystem starSystem = Game.Systems[entityGuid];
            SendSystemData(sender, starSystem);
            string name = starSystem.NameDB.DefaultName;
            Messages.Add("Sent System " + name);
        }

        void HandleRequestEntityData(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            Entity entity;
            if (this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                SendEntityData(sender, entity);
            else
                Messages.Add(sender.ToString() + "EntityDataRequestFail: No Entity for Guid: " + entityGuid);
        }

        void HandleRequestEntityHash(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            Entity entity;
            if (this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                SendEntityHashData(sender, entity);
            else
                Messages.Add(sender.ToString() + "EntityHashRequestFail: No Entity for Guid: " + entityGuid);
            
        }



        void HandleRequestDatablob(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            string datablobTypename = message.ReadString();
            int datablobTypeIndex = message.ReadInt32();

            Entity entity;
            if (!this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                Messages.Add(sender.ToString() + "DatablobRequestFail: No Entity for Guid: " + entityGuid);
            else
            {
                var datablob = entity.GetDataBlob<BaseDataBlob>(datablobTypeIndex);
                Messages.Add("Requested Typename: " + datablobTypename);
                Messages.Add("Type by Index(" + datablobTypeIndex +"): " + datablob.GetType());

                SendDatablob(sender, entity, datablob);
            }

        }


        #endregion

        #region SendMessages & data

        void SendTickInfo(DateTime newDate)
        {
            if (_connectedFactions.Count > 0)
            {
                TimeSpan deltaTime = newDate - _currentDate;
                //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Messages.Add("TickEvent: CurrentTime: " + currentTime + " Delta: " + delta)));
                //Messages.Add("TickEvent: CurrentTime: " + currentTime + " Delta: " + delta);
                IList<NetConnection> connections = _connectedFactions.Keys.ToList();
                NetOutgoingMessage sendMsg = NetServerObject.CreateMessage();
                sendMsg.Write((byte)ToClientMsgType.TickInfo);
                sendMsg.Write(newDate.ToBinary());
                sendMsg.Write(_currentDate.ToBinary());
                NetServerObject.SendMessage(sendMsg, connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
            _currentDate = newDate;
        }

        void SendGameSettings(NetConnection recipient, Game game)
        {
            var gamesettings = game.Settings;
            var mStream = new MemoryStream();
            SerializationManager.Export(game.Settings, mStream);

            byte[] byteArray = mStream.ToArray();

            int len = byteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendGameData);
            sendMsg.Write(len);
            sendMsg.Write(byteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);

        }

        void SendFactionHash(NetConnection recipient, Entity factionEntity)
        {
            ProtoEntity factionEntityClone = factionEntity.Clone(); //clone it, then remove the AuthDB, we don't send it, so don't include the hash for it. 
            factionEntityClone.RemoveDataBlob<AuthDB>();

            MemoryStream mStream = new MemoryStream();
            NetOutgoingMessage netMessage = NetPeerObject.CreateMessage();
            netMessage.Write((byte)ToClientMsgType.SendEntityHashData);

            List<BaseDataBlob> blobsWithValueHash = (List<BaseDataBlob>)factionEntityClone.DataBlobs.Where((arg) => arg is IGetValuesHash);
            netMessage.Write(blobsWithValueHash.Count);


            foreach (IGetValuesHash item in blobsWithValueHash)
            {
                netMessage.Write(item.GetType().ToString());
                netMessage.Write(item.GetValueCompareHash());
            }
            NetServerObject.SendMessage(netMessage, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        void SendFactionData(NetConnection recipient, Entity factionEntity)
        {

            //var ownedEntities = factionEntity.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites.Values.ToArray();
            ProtoEntity factionEntityClone = factionEntity.Clone(); //clone it, then remove the AuthDB, we don't want to send that.
            factionEntityClone.RemoveDataBlob<AuthDB>();

            var mStream = new MemoryStream(); 
            SerializationManager.Export(Game, mStream, factionEntityClone);


            byte[] entityByteArray = mStream.ToArray();
            int len = entityByteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendFactionEntity); 
            sendMsg.Write(factionEntity.Guid.ToByteArray());
            sendMsg.Write(factionEntityClone.GetValueCompareHash());
            sendMsg.Write(len);
            sendMsg.Write(entityByteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
            mStream.Close();

        }

        void SendEntityHashData(NetConnection recipient, Entity entity)
        {   //TODO: check which thread is doing this, and at what time, this info should probilby be done by the managersubpulse thread at a specific time.
            MemoryStream mStream = new MemoryStream();
            NetOutgoingMessage netMessage = NetPeerObject.CreateMessage();
            netMessage.Write((byte)ToClientMsgType.SendEntityHashData);
            netMessage.Write(entity.Guid.ToByteArray());                                        //Entity Guid
            netMessage.Write(entity.Manager.ManagerSubpulses.SystemLocalDateTime.ToBinary());   //Date
            netMessage.Write(entity.GetHashCode());                                             //Entity Hash
            netMessage.Write(entity.DataBlobs.Count);                                           //NumberOfDatablobs
            foreach (IGetValuesHash item in entity.DataBlobs)                                   //
            {
                netMessage.Write(item.GetType().ToString());                                    //Datablob Type Name
                netMessage.Write(item.GetValueCompareHash());                                   //Datablob Hash
            }
            NetServerObject.SendMessage(netMessage, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        void SendEntityData(NetConnection recipient, Entity entity)
        {

            var mStream = new MemoryStream();
            SerializationManager.Export(Game, mStream, entity);


            byte[] entityByteArray = mStream.ToArray();
            int len = entityByteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendEntity); //receving it is the same as any normal entity.
            sendMsg.Write(entity.Guid.ToByteArray());
            sendMsg.Write(entity.GetValueCompareHash());
            sendMsg.Write(len);
            sendMsg.Write(entityByteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
            mStream.Close();
        }

        void SendSystemData(NetConnection recipient, StarSystem starSystem)
        {
            var mStream = new MemoryStream();
            SerializationManager.Export(Game, mStream, starSystem);
            byte[] systemByteArray = mStream.ToArray();
            int len = systemByteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendSystemData);

            sendMsg.Write(starSystem.Guid.ToByteArray());
            sendMsg.Write(len);
            sendMsg.Write(systemByteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        void SendDatablob(NetConnection recipient, Entity entity, BaseDataBlob datablob)
        {
            //string typename = datablobtype.AssemblyQualifiedName;

            var mStream = new MemoryStream();
            //int typeIndex = EntityManager.DataBlobTypes[datablobtype];
            //var datablob = entity.GetDataBlob<BaseDataBlob>(typeIndex);

            byte[] systemByteArray = mStream.ToArray();
            int len = systemByteArray.Length;

            Messages.Add("GetType().ToSTring(): " + datablob.GetType().ToString());
            Messages.Add("GetType().Name: " + datablob.GetType().Name);
            Messages.Add("GetType().AssemblyQualifiedName: " + datablob.GetType().AssemblyQualifiedName);
            Messages.Add("GetType().FullName: " + datablob.GetType().FullName);
            //Messages.Add("pulsarTypeIndex: " + typeIndex);

            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToClientMsgType.SendDatablob);
            msg.Write(entity.Guid.ToByteArray());
            msg.Write(datablob.GetType().ToString());
            msg.Write(systemByteArray);
            NetServerObject.SendMessage(msg, recipient, NetDeliveryMethod.ReliableOrdered);



        }

        #endregion

        #region Other


        //private void SendFactionList(NetConnection recipient)
        //{
        //    //list of factions: 

        //    List<Entity> factions = Game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();
        //    //we don't want to send the whole entitys, just a dictionary of guid ID and the string name. 
        //    //Dictionary<Guid,string> factionGuidNames = factions.ToDictionary(faction => faction.Guid, faction => faction.GetDataBlob<NameDB>().DefaultName);

        //    List<FactionItem> factionItems = new List<FactionItem>();
        //    foreach (var faction in factions)
        //    {
        //        FactionItem factionItem = new FactionItem();
        //        factionItem.Name = faction.GetDataBlob<NameDB>().DefaultName;
        //        factionItem.ID = faction.Guid;
        //        factionItems.Add(factionItem);
        //    }

        //    DataMessage dataMessage = new DataMessage { DataMessageType = DataMessageType.FactionDictionary, DataObject = factionItems };

        //    NetOutgoingMessage sendMsg = SerialiseDataMessage(dataMessage);

        //    Messages.Add("TX Faction List to " + recipient.RemoteUniqueIdentifier);
        //    NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
        //}
        #endregion
    }
}
