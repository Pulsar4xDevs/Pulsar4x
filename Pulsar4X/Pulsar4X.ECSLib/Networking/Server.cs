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
        SendTickInfo,
        SendStringMessage,
        SendGameData,
        SendEntity,
        SendEntityHashData,
        SendDatablob,
        SendSystemData,
        SendFactionEntity,
        SendEntityCommandAck,
        SendEntityChangeData,
        SendEntityProcData
    }

    public class NetworkHost : NetworkBase, IOrderHandler
    {

        private Dictionary<NetConnection, Guid> _connectedFactions { get; } = new Dictionary<NetConnection, Guid>();
        internal Dictionary<Guid, List<NetConnection>> FactionConnections { get; } = new Dictionary<Guid, List<NetConnection>>();
        internal Dictionary<Guid, NetEntityChangeListner> FactionEntityListners { get; } = new Dictionary<Guid, NetEntityChangeListner>();
        public NetServer NetServerObject { get { return (NetServer)NetPeerObject; } }

        //public Game Game { get; set; }

        private DateTime _currentDate;



        public NetworkHost(Game game, int portNum)
        {
            PortNum = portNum;
            Game = game;
            game.OrderHandler = this;
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
            if (!FactionConnections.ContainsKey(faction.Guid))
            {
                FactionConnections.Add(faction.Guid, new List<NetConnection>());
            }
            if (!FactionConnections[faction.Guid].Contains(netCon))
                FactionConnections[faction.Guid].Add(netCon);
            if (!FactionEntityListners.ContainsKey(faction.Guid))
                FactionEntityListners[faction.Guid] = new NetEntityChangeListner(Game.GlobalManager, faction);
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
                FactionConnections[factionGuid].Remove(netCon);
                //if (_factionConnections[factionGuid].Count == 0) //don't remove, we'll keep the list of changes and TODO: can send that on reconnect. 
                //    _factionEntityListners.Remove(factionGuid);
            }

        }

        private void NetHostStart()
        {
            var config = new NetPeerConfiguration("Pulsar4X") { Port = PortNum };
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);

            NetPeerObject = new NetServer(config);

            NetPeerObject.Start();

            StartListning();
            Game.GamePulse.GameGlobalDateChangedEvent += SendTickInfo;
        }

        protected override void HandleDiscoveryRequest(NetIncomingMessage message)
        {

            Messages.Add("RX DiscoveryRequest " + message.SenderEndPoint);
            NetOutgoingMessage response = NetServerObject.CreateMessage();
            response.Write("Pulsar4x Game");//Game.GameName);
            response.Write(StaticRefLib.CurrentDateTime.ToBinary());

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
                case ToServerMsgType.SendPlayerEntityCommand:
                    HandleEntityCommand(message);
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

            if (factions.Exists(item => item.GetDataBlob<NameDB>().DefaultName == name))
            {
                Entity faction = factions.Find(item => item.GetDataBlob<NameDB>().DefaultName == name);

                if (AuthProcessor.Validate(faction, pass))
                {
                    AddFactionNetconLink(faction, sender);
                    SendGameSettings(sender, Game);
                    //TODO negotiate and get a delta between the server's data and the client's data, and only update the client with changed data.
                    //(the client should be able to save/keep a cashe of it's own data so there's less to send when reconnecting to a server.)
                    SendFactionData(sender, faction);
                    Messages.Add("Sent Faction " + name);
                }
                else
                {
                    SendStringMessage(sender, "Server could not auth faction, check your password and try again"); //TODO: should probibly check the number of bad passwords and handle that properly
                }
            }
            else
            {
                SendStringMessage(sender, "Server could not find faction with name: " + name);
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
                Messages.Add(sender.ToString() + "EntityDataRequestFail: No Entity for ID: " + entityGuid);
        }

        void HandleRequestEntityHash(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            Entity entity;
            if (this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                SendEntityHashData(sender, entity);
            else
                Messages.Add(sender.ToString() + "EntityHashRequestFail: No Entity for ID: " + entityGuid);
            
        }

        void HandleRequestDatablob(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            Guid entityGuid = new Guid(message.ReadBytes(16));
            string datablobTypename = message.ReadString();
            int datablobTypeIndex = message.ReadInt32();

            Entity entity;
            if (!this.Game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                Messages.Add(sender + "DatablobRequestFail: No Entity for ID: " + entityGuid);
            else
            {
                var datablob = entity.GetDataBlob<BaseDataBlob>(datablobTypeIndex);
                if (datablob.GetType().Name != datablobTypename)
                {
                    Messages.Add("Requested DatablobTypeIndex and TypeName do not match! this could indicate a data missmatch between server and client, possibly a version difference.");
                    Messages.Add("Requested Typename: " + datablobTypename);
                    Messages.Add("Type by Index(" + datablobTypeIndex + "): " + datablob.GetType().Name);
                }
                else
                {
                    SendDatablob(sender, entity, datablob);
                }
            }
        }

        void HandleEntityCommand(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;

            Guid cmdID = new Guid(message.ReadBytes(16));
            string cmdTypeName = message.ReadString();
            int len = message.ReadInt32();
            byte[] data = message.ReadBytes(len);
            var mStream = new MemoryStream(data);
            string fullName = "Pulsar4X.ECSLib." + cmdTypeName;
            Type cmdType = Type.GetType(fullName);
            if (cmdType == null)
                Messages.Add("Command Type: " + fullName + " not found. this could be caused by the client having a missmatched game version"); //TODO: send this back to the client.
            else
            {
                EntityCommand cmd = SerializationManager.ImportEntityCommand(Game, cmdType, mStream);
                cmd.CmdID = cmdID;
                Messages.Add("Reseved Command: " + cmdID);
                HandleOrder(cmd);
            }
        }

        #endregion

        #region SendMessages & data

        void SendTickInfo(DateTime newDate)
        {
            if (_connectedFactions.Count > 0)
            {
                foreach (var kvp in FactionEntityListners)
                {
                    var recipients = FactionConnections[kvp.Key];
                    NetOutgoingMessage sendentityChangeMsg = NetServerObject.CreateMessage();
                    EntityChangeData changeData;
                    while( kvp.Value.TryDequeue(out changeData)) //this is dequeuing from the global manager
                    {
                        SendEntityChangeData(recipients, changeData);
                    }
                    foreach (var managerListner in kvp.Value.ManagerListners) //loop through the system manager listners that the faction knows about. 
                    {
                        while (managerListner.TryDequeue(out changeData)) //this is dequeing from a system manager
                        {
                            SendEntityChangeData(recipients, changeData);
                        }
                    }
                }


                TimeSpan deltaTime = newDate - _currentDate;
                Messages.Add("TickEvent: from: " + _currentDate + " to: " + newDate + " Delta: " + deltaTime);
                IList<NetConnection> connections = _connectedFactions.Keys.ToList();
                NetOutgoingMessage sendMsg = NetServerObject.CreateMessage();
                sendMsg.Write((byte)ToClientMsgType.SendTickInfo);
                sendMsg.Write(newDate.ToBinary());
                sendMsg.Write(_currentDate.ToBinary());
                NetServerObject.SendMessage(sendMsg, connections, NetDeliveryMethod.ReliableOrdered, 0);

            }
            _currentDate = newDate;
        }

        void SendEntityChangeData(List<NetConnection> recipients, EntityChangeData changeData)
        {
            NetOutgoingMessage message = NetPeerObject.CreateMessage();
            message.Write((byte)ToClientMsgType.SendEntityChangeData);
            message.Write((byte)changeData.ChangeType);
            switch (changeData.ChangeType)
            {
                case EntityChangeData.EntityChangeType.EntityAdded:
                    {
                        EntityDataMessage(message, changeData.Entity);
                    }
                    break;
                case EntityChangeData.EntityChangeType.EntityRemoved:
                    {
                        message.Write(changeData.Entity.Guid.ToByteArray());
                    }
                    break;
                case EntityChangeData.EntityChangeType.DBAdded:
                    {
                        DatablobDataMessage(message, changeData.Entity, changeData.Datablob);
                    }
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    { 
                        message.Write(changeData.Entity.Guid.ToByteArray());                        //entity guid. 
                        message.Write(changeData.Datablob.GetType().Name);                          //datablob name
                        message.Write(EntityManager.DataBlobTypes[changeData.Datablob.GetType()]);  //datablob typeIndex
                    }
                    break;
                default:
                    throw new Exception("Network classes need to handle EntityChangeType");
            }
            foreach (var recipient in recipients)
            {
                NetServerObject.SendMessage(message, recipient, NetDeliveryMethod.ReliableOrdered);
            }
            Messages.Add("Sent " + changeData.ChangeType + " changeData to " + recipients.Count + " netclient."); 

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
            sendMsg.Write(StaticRefLib.CurrentDateTime.ToBinary());
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
            netMessage.Write(entity.Guid.ToByteArray());                                        //Entity ID
            netMessage.Write(entity.Manager.ManagerSubpulses.StarSysDateTime.ToBinary());   //Date
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
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendEntity); //receving it is the same as any normal entity.
            EntityDataMessage(sendMsg, entity);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
            SendEntityProcData(recipient, entity);

        }

        NetOutgoingMessage EntityDataMessage(NetOutgoingMessage msg, Entity entity)
        {
            var mStream = new MemoryStream();
            SerializationManager.Export(Game, mStream, entity);
            byte[] entityByteArray = mStream.ToArray();
            int len = entityByteArray.Length;
            msg.Write(entity.Guid.ToByteArray());
            msg.Write(entity.GetValueCompareHash());
            msg.Write(len);
            msg.Write(entityByteArray);

            mStream.Close();
            return msg;
        }

        void SendEntityProcData(NetConnection recipient, Entity entity)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToClientMsgType.SendEntityProcData); 
            var procDict = entity.Manager.ManagerSubpulses.GetInstanceProcForEntity(entity);
            var mStream = new MemoryStream();
            SerializationManager.Export(procDict, mStream);
            byte[] procdicByteArray = mStream.ToArray();
            int len = procdicByteArray.Length;
            msg.Write(entity.Guid.ToByteArray());
            msg.Write(len);
            msg.Write(procdicByteArray);
            mStream.Close();

            NetServerObject.SendMessage(msg, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        void SendSystemData(NetConnection recipient, StarSystem starSystem)
        {
            Entity faction = Game.GlobalManager.GetGlobalEntityByGuid(_connectedFactions[recipient]);
            List<Entity> ownedEntitiesForSystem = starSystem.GetEntitiesByFaction(faction.Guid);

            var mStream = new MemoryStream();


            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)ToClientMsgType.SendSystemData);

            sendMsg.Write(starSystem.Guid.ToByteArray());

            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);

            foreach (var entity in ownedEntitiesForSystem)
            {
                SendEntityData(recipient, entity);
            }
        }

        void SendDatablob(NetConnection recipient, Entity entity, BaseDataBlob datablob)
        {
            Messages.Add("Sending " + datablob.GetType().Name);
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToClientMsgType.SendDatablob);      //message type
            NetServerObject.SendMessage(msg, recipient, NetDeliveryMethod.ReliableOrdered);

        }

        NetOutgoingMessage DatablobDataMessage(NetOutgoingMessage msg, Entity entity, BaseDataBlob datablob)
        {

            var mStream = new MemoryStream();
            //int typeIndex = EntityManager.DataBlobTypes[datablobtype];
            //var datablob = entity.GetDataBlob<BaseDataBlob>(typeIndex);

            //Messages.Add("GetType().ToSTring(): " + datablob.GetType().ToString());
            //Messages.Add("GetType().Name: " + datablob.GetType().Name);
            //Messages.Add("GetType().AssemblyQualifiedName: " + datablob.GetType().AssemblyQualifiedName);
            //Messages.Add("GetType().FullName: " + datablob.GetType().FullName);
            //Messages.Add("pulsarTypeIndex: " + typeIndex);

            SerializationManager.Export(Game, mStream, datablob);
            byte[] systemByteArray = mStream.ToArray();
            int len = systemByteArray.Length;

            msg.Write(entity.Guid.ToByteArray());                   //entityGuid
            msg.Write(datablob.GetType().Name);                     //datablob name
            msg.Write(EntityManager.DataBlobTypes[datablob.GetType()]);//datablob typeIndex
            msg.Write(len);                                         //stream length
            msg.Write(systemByteArray);                             //encoded data.
            mStream.Close();
            return msg;
        }

        void SendEntityCommandAck(NetConnection recipient, Guid cmdID, bool isValid)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToClientMsgType.SendEntityCommandAck);
            msg.Write(cmdID.ToByteArray());
            msg.Write(isValid);
            Messages.Add("Sending " + isValid + " Command ack ID: " + cmdID);
            NetServerObject.SendMessage(msg, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        void SendStringMessage(NetConnection recipient, string message)
        {
            NetOutgoingMessage msg = NetPeerObject.CreateMessage();
            msg.Write((byte)ToClientMsgType.SendStringMessage);
            msg.Write(message);
            NetServerObject.SendMessage(msg, recipient, NetDeliveryMethod.ReliableOrdered);
        }

        public void HandleOrder(EntityCommand entityCommand) //public because it's an interface. 
        {
            if (entityCommand.IsValidCommand(Game))
            {
                if (FactionConnections.ContainsKey(entityCommand.RequestingFactionGuid))
                {
                    foreach (var item in FactionConnections[entityCommand.RequestingFactionGuid])
                    {
                        SendEntityCommandAck(item, entityCommand.CmdID, true);
                    }
                }
                var orderableDB = entityCommand.EntityCommanding.GetDataBlob<OrderableDB>();
                orderableDB.AddCommandToList(entityCommand);
                orderableDB.ProcessOrderList(entityCommand.EntityCommanding.StarSysDateTime);
            }
        } 

        #endregion

        #region Other


        //private void SendFactionList(NetConnection recipient)
        //{
        //    //list of factions: 

        //    List<Entity> factions = Game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();
        //    //we don't want to send the whole entitys, just a dictionary of guid ID and the string name. 
        //    //Dictionary<ID,string> factionGuidNames = factions.ToDictionary(faction => faction.ID, faction => faction.GetDataBlob<NameDB>().DefaultName);

        //    List<FactionItem> factionItems = new List<FactionItem>();
        //    foreach (var faction in factions)
        //    {
        //        FactionItem factionItem = new FactionItem();
        //        factionItem.Name = faction.GetDataBlob<NameDB>().DefaultName;
        //        factionItem.ID = faction.ID;
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
