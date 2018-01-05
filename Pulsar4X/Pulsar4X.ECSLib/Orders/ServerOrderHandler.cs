using System;
using System.Collections.Generic;
using Lidgren.Network;
using Pulsar4X.Networking;

namespace Pulsar4X.ECSLib
{
    /*
    internal class ServerOrderHandler : OrderHandler
    {


        internal NetworkHost NetHost;

        internal ServerOrderHandler(Game game, int portNum) : base(game)
        {
            NetHost = new NetworkHost(game, portNum);
        }

        internal override void HandleOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(_game))
            {
                if (NetHost.FactionConnections.ContainsKey(entityCommand.RequestingFactionGuid))
                {
                    foreach (var item in NetHost.FactionConnections[entityCommand.RequestingFactionGuid])
                    {
                        NetHost.SendEntityCommandAck(item, entityCommand.CmdID, true);
                    }
                }

                entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList.Add(entityCommand);
                var commandList = entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList;
                OrderableProcessor.ProcessOrderList(_game, commandList);
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
            _netClient.SendEntityCommand(entityCommand);
        }
    }
    */
}
