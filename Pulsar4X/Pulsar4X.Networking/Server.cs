using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Lidgren.Network;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Networking
{
    public class NetworkHost : NetworkBase
    {
        private Dictionary<NetConnection, Guid> _connectedFactions { get; set; }
        private Dictionary<Guid, List<NetConnection>> _factionConnections { get; set; }
        public NetServer NetServerObject { get { return (NetServer)NetPeerObject; } }



        public NetworkHost(int portNum)
        {
            PortNum = portNum;
        }

        public void ServerStart()
        {
            var config = new NetPeerConfiguration("Pulsar4X") { Port = PortNum };
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);

            NetPeerObject = new NetServer(config);
            NetPeerObject.Start();
            _connectedFactions = new Dictionary<NetConnection, Guid>();
            _factionConnections = new Dictionary<Guid, List<NetConnection>>();
            EntityDataSanitiser.Initialise(Game);
            StartListning();
            SetSendMessages();
        }

        private void OnTickEvent(DateTime currentTime, int delta)
        {
            if (_connectedFactions.Count > 0)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Messages.Add("TickEvent: CurrentTime: " + currentTime + " Delta: " + delta)));
                //Messages.Add("TickEvent: CurrentTime: " + currentTime + " Delta: " + delta);
                IList<NetConnection> connections = _connectedFactions.Keys.ToList();
                NetOutgoingMessage sendMsg = NetServerObject.CreateMessage();
                sendMsg.Write((byte)DataMessageType.TickInfo);
                sendMsg.Write(currentTime.ToBinary());
                sendMsg.Write(delta);
                NetServerObject.SendMessage(sendMsg, connections, NetDeliveryMethod.ReliableOrdered, 0);
                
            }

        }

        protected override void HandleDiscoveryRequest(NetIncomingMessage message)
        {
            //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Messages.Add("RX DiscoveryRequest " + message.SenderEndPoint)));

            Messages.Add("RX DiscoveryRequest " + message.SenderEndPoint);
            NetOutgoingMessage response = NetServerObject.CreateMessage();
            response.Write(Game.GameName);
            response.Write(Game.CurrentDateTime.ToBinary());

            NetServerObject.SendDiscoveryResponse(response, message.SenderEndPoint);
        }

        protected override void HandleEntityData(NetIncomingMessage message)
        {
            //EntityData, (Byte[])Guid, (Byte[])memoryStream
            NetConnection recipient = message.SenderConnection;
            Guid entityID = new Guid(message.ReadBytes(16));
            Entity reqestedEntity = Game.GlobalManager.GetEntityByGuid(entityID);
            ProtoEntity sanitisedEntity = EntityDataSanitiser.SanitisedEntity(reqestedEntity, _connectedFactions[recipient]);
            //TODO check that this faction is allowed to access this entity's Data.
            var mStream = new MemoryStream();
            SaveGame.ExportEntity(sanitisedEntity, mStream);
            byte[] entityByteArray = mStream.ToArray();

            int len = entityByteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)DataMessageType.EntityData);
            sendMsg.Write(reqestedEntity.Guid.ToByteArray());
            sendMsg.Write(len);
            sendMsg.Write(entityByteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
        }


        protected override void HandleFactionData(NetIncomingMessage message)
        {
            NetConnection sender = message.SenderConnection;
            string name = message.ReadString();
            string pass = message.ReadString();
            List<Entity> factions = Game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();

            Entity faction = factions.Find(item => item.GetDataBlob<NameDB>().DefaultName == name);

            if (AuthProcessor.Validate(faction, pass))
            {
                if (_connectedFactions.ContainsKey(sender))
                    _connectedFactions[sender] = faction.Guid;
                else
                    _connectedFactions.Add(sender, faction.Guid);
                if (!_factionConnections.ContainsKey(faction.Guid))
                    _factionConnections.Add(faction.Guid, new List<NetConnection>());
                _factionConnections[faction.Guid].Add(sender);
                SendFactionData(sender, faction);
            }
        }

        protected override void ConnectionStatusChanged(NetIncomingMessage message)
        {
            switch (message.SenderConnection.Status)
            {
                case NetConnectionStatus.Connected:
                    break;
                case NetConnectionStatus.Disconnected:
                    if (_connectedFactions.ContainsKey(message.SenderConnection))
                    {
                        Guid factionGuid = _connectedFactions[message.SenderConnection];
                        _factionConnections[factionGuid].Remove(message.SenderConnection);
                        _connectedFactions.Remove(message.SenderConnection);
                    }
                    break;
            }
        }

        private void SetSendMessages()
        {
            Game.TickStartEvent += OnTickEvent;
        }

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



        private void SendFactionData(NetConnection recipient, Entity factionEntity)
        {

            var mStream = new MemoryStream();
            SaveGame.ExportEntity(factionEntity, mStream);
            byte[] entityByteArray = mStream.ToArray();

            int len = entityByteArray.Length;
            NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
            sendMsg.Write((byte)DataMessageType.FactionData);
            sendMsg.Write(factionEntity.Guid.ToByteArray());
            sendMsg.Write(len);
            sendMsg.Write(entityByteArray);
            NetServerObject.SendMessage(sendMsg, recipient, NetDeliveryMethod.ReliableOrdered);
            foreach (var systemID in factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems)
            {
                mStream = new MemoryStream();
                SaveGame.ExportStarSystem(Misc.LookupStarSystem(Game.Systems, systemID), mStream);
                byte[] byteArray = mStream.ToArray();
                len = byteArray.Length;
                NetOutgoingMessage sendMsgSystem = NetPeerObject.CreateMessage();
                sendMsgSystem.Write((byte)DataMessageType.SystemData);
                sendMsgSystem.Write(systemID.ToByteArray());
                sendMsgSystem.Write(len);
                sendMsgSystem.Write(byteArray);
                NetServerObject.SendMessage(sendMsgSystem, recipient, NetDeliveryMethod.ReliableOrdered);
            }
        }


    }
}
