using System;
using System.Collections.Generic;
namespace Pulsar4X.ECSLib
{
    internal abstract class OrderHandler
    {
        internal Game _game;

        internal OrderHandler(Game game)
        { _game = game; }

        internal abstract void HandleOrder(EntityCommand entityCommand);
    }


    internal class StandAloneOrderHandler:OrderHandler
    {
        internal StandAloneOrderHandler(Game game) : base(game)
        {
        }

        internal override void HandleOrder(EntityCommand entityCommand)
        {
            if (entityCommand.IsValidCommand(_game))
            {
                entityCommand.EntityCommanding.GetDataBlob<OrderableDB>().ActionList.Add(entityCommand);
                OrderableProcessor.ProcessOrderList(entityCommand.EntityCommanding.GetDataBlob<OrderableDB>());
            }              
        }
    }


    internal class ServerOrderHandler:OrderHandler
    {
        bool HasClients = false;


        Dictionary<Guid, List<RemoteConnection>> factionListners = new Dictionary<Guid, List<RemoteConnection>>();

        internal ServerOrderHandler(Game game) : base(game)
        {
        }

        internal override void HandleOrder(EntityCommand entityCommand)
        {
            RXOrder(entityCommand);
        }

        internal void RXOrder(EntityCommand entityCommand)
        {
            if(entityCommand.IsValidCommand(_game) && HasClients)
            {
                if(factionListners.ContainsKey(entityCommand.RequestingFactionGuid))
                {
                    foreach(var item in factionListners[entityCommand.RequestingFactionGuid])
                    {
                        item.SendOrder(entityCommand);
                    }
                }
            }
        }
    }

    internal class ClientOrderHandler:OrderHandler
    {
        RemoteConnection ServerConnection;

        internal ClientOrderHandler(Game game) : base(game)
        {
        }
        internal override void HandleOrder(EntityCommand entityCommand)
        {
            TXOrder(entityCommand);
        }
        internal void TXOrder(EntityCommand entityCommand)
        {
            ServerConnection.SendOrder(entityCommand);
        }
        internal void RXOrder(EntityCommand entityCommand)
        {
            entityCommand.ActionCommand(_game);
        }


    }


    internal class RemoteConnection //flesh this out once I've got lidgren back in
    {
        internal void SendOrder(EntityCommand entityCommand)
        { }


    }
}
