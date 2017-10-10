using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Lidgren.Network;
using Pulsar4X.ECSLib;



namespace Pulsar4X.Networking
{


    public enum DataMessageType : byte
    {
        StringMessage,
        FactionDictionary,
        GameData,
        FactionData,
        SystemData,
        EntityData,
        DataBlobData,
        DataBlobPropertyData,
        

        TickInfo
    }

    /*Messages look like:
     * 
     * GameData, (string)gameName, (long)currentDate
     * SystemData, (Byte[])Guid, (Byte[])memoryStream
     * EntityData, (Byte[])Guid, (Byte[])memoryStream
     * 
     * 
     * FactionDataRequest, (string)factionName, (string)password
     * TickInfo, (long)fromDate, (long)Delta
     * 
     */

    //[Serializable]
    //public class DataMessage
    //{
    //    public DataMessageType DataMessageType { get; set; }
    //    public object DataObject { get; set; }

    //    public Guid EntityGuid { get; set; }
    //    public string PropertyName { get; set; }//actualy can I look at how wpf does this?
    //}

    /// <summary>
    /// a short struct which holds string name and guid of a faction. 
    /// </summary>
    [Serializable]
    public struct FactionItem
    {
        public string Name { get; set; }
        public Guid ID { get; set; }
    }

    public class NetworkBase
    {
        public Game Game { get; set; }
        //protected GameVM _gameVM_ { get; set; }
        protected const int SecureChannel = 31;
        private readonly ObservableCollection<string> _messages;
        public ObservableCollection<string> Messages { get { return _messages; } }

        public int PortNum { get; set; }
        public NetPeer NetPeerObject { get; set; }

        public NetworkBase()
        {
            _messages = new ObservableCollection<string>();
            _messages.Add("Start of Messages");
        }

        protected void StartListning()
        {
            NetPeerObject.RegisterReceivedCallback(new SendOrPostCallback(GotMessage));
        }

        public void Shutdown(string bye)
        {
            NetPeerObject.Shutdown(bye);
        }

        /// <summary>
        /// This gets called when triggered by an event regestered in StartListning()
        /// </summary>
        /// <param name="peer">this is NetPeerObject so just using that</param>
        public void GotMessage(object peer)
        {
            NetIncomingMessage message;

            while ((message = NetPeerObject.ReadMessage()) != null)
            {
                if (message.SequenceChannel == SecureChannel)
                {
                    DecryptedReceve(message);
                }
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        Messages.Add("Data Message from: " + message.SenderConnection.RemoteUniqueIdentifier);
                        HandleIncomingDataMessage(message.SenderConnection, message);
                        break;

                    case NetIncomingMessageType.DiscoveryRequest:
                        HandleDiscoveryRequest(message);
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        HandleDiscoveryResponse(message);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        Messages.Add("New status: " + message.SenderConnection.Status + " (Reason: " + message.ReadString() + ")");
                        ConnectionStatusChanged(message);
                        break;
                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        Messages.Add("Debug Msg: " + message.ReadString());
                        break;

                    /* .. */
                    default:
                        Messages.Add(("unhandled message with type: " + message.MessageType));
                        break;
                }
            }
            PostQueueHandling();
        }
        
        /// <summary>
        /// TODO implement proper private/public key. is that even possible to do transperantly?
        /// </summary>
        /// <param name="recever"></param>
        /// <param name="message"></param>
        protected NetOutgoingMessage Encrypt(NetOutgoingMessage message)
        {
            NetEncryption algo = new NetXtea(NetPeerObject, "SharedKey45B635DF-649B-4C10-B110-439CE1784C59");
            message.Encrypt(algo);
            return message;
        }


        protected NetIncomingMessage DecryptedReceve(NetIncomingMessage message)
        {
            NetEncryption algo = new NetXtea(NetPeerObject, "SharedKey45B635DF-649B-4C10-B110-439CE1784C59");
            message.Decrypt(algo);
            return message;
        }

        protected virtual void PostQueueHandling()
        {
        }

        protected virtual void HandleDiscoveryRequest(NetIncomingMessage message)
        {
        }
        protected virtual void HandleDiscoveryResponse(NetIncomingMessage message)
        {
        }
        protected virtual void ConnectionStatusChanged(NetIncomingMessage message)
        {
        }
        protected void HandleIncomingDataMessage(NetConnection sender, NetIncomingMessage message)
        {
            DataMessageType messageType = (DataMessageType)message.ReadByte();
            switch (messageType)
            {
                case DataMessageType.FactionData:
                    HandleFactionData(message);
                    break;
                case DataMessageType.GameData:
                    HandleGameDataMessage(message);
                    break;
                case DataMessageType.TickInfo:
                    HandleTickInfo(message);
                    break;
                case DataMessageType.SystemData:
                    HandleSystemData(message);
                    break;
                case DataMessageType.EntityData:
                    HandleEntityData(message);
                    break;
            }
        }

        /// <summary>
        /// server only, server sends data back EntityData and SystemData meessages
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleFactionData(NetIncomingMessage message)
        {
        }


        /// <summary>
        /// server reads this as a request. client reads this as data
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleSystemData(NetIncomingMessage message)
        {
        }

        /// <summary>
        /// server reads this as a request. client reads this as data
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleEntityData(NetIncomingMessage message)
        {
        }

        /// <summary>
        /// client read only: this happens when a server advances time
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleTickInfo(NetIncomingMessage message)
        {
        }



        /// <summary>
        /// client read only
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGameDataMessage(NetIncomingMessage message)
        {
        }


        /// <summary>
        /// use this for sending an non basic object type.
        /// </summary>
        /// <param name="dataMessage"></param>
        /// <returns></returns>
        //protected NetOutgoingMessage SerialiseDataMessage(DataMessage dataMessage)
        //{
        //    //turn the dataMessage into a stream.
        //    var binFormatter = new BinaryFormatter();
        //    var mStream = new MemoryStream();
        //    binFormatter.Serialize(mStream, dataMessage);
        //    NetOutgoingMessage sendMsg = NetPeerObject.CreateMessage();
        //    sendMsg.Write(mStream.ToArray()); //send the stream as an byte array. 
        //    return sendMsg;
        //}

        //protected void HandleIncomingStringMessage(NetConnection sender, DataMessage dataMessage)
        //{
        //    Messages.Add(sender.RemoteUniqueIdentifier + " Sent: " + (string)dataMessage.DataObject);
        //}

    }
}
