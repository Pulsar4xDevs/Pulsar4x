using System;
using System.Collections.Generic;
using Lidgren.Network;
using Pulsar4X.Networking;

namespace Pulsar4X.ECSLib
{
    internal class ServerOrderHandler : OrderHandler
    {
        bool HasClients = false;

        NetworkHost _netHost;

        //Dictionary<Guid, List<RemoteConnection>> factionListners = new Dictionary<Guid, List<RemoteConnection>>();

        internal ServerOrderHandler(Game game, int portNum) : base(game)
        {
            _netHost = new NetworkHost(portNum);
            _netHost.ServerStart();
        }

        internal override void HandleOrder(EntityCommand entityCommand)
        {
            RXOrder(entityCommand);
        }

        internal void RXOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(_game) && HasClients)
            {
                /*
                if (factionListners.ContainsKey(entityCommand.RequestingFactionGuid))
                {
                    foreach (var item in factionListners[entityCommand.RequestingFactionGuid])
                    {
                        item.SendOrder(entityCommand);
                    }
                }*/
            }
        }
    }


    internal class ClientOrderHandler : OrderHandler
    {
        NetworkClient _netClient;


        internal ClientOrderHandler(Game game, NetworkClient netClient) : base(game)
        {
            _netClient = netClient;
        }
        internal override void HandleOrder(EntityCommand entityCommand)
        {

            NetOutgoingMessage msg = _netClient.NetPeerObject.CreateMessage();
            //msg.Data = entityCommand;
            //_netClient..SendMessage(msg, NetDeliveryMethod.Unreliable);
            TXOrder(entityCommand);
        }
        internal void TXOrder(EntityCommand entityCommand)
        {
            //ServerConnection.SendOrder(entityCommand);
        }
        internal void RXOrder(EntityCommand entityCommand)
        {
            entityCommand.ActionCommand(_game);
        }


    }
}
