using System;
using System.Collections.Generic;
namespace Pulsar4X.ECSLib
{
    internal abstract class OrderHandler
    {
        internal Game _game;

        internal OrderHandler(Game game)
        { _game = game; }

        internal abstract void HandleOrder(IEntityCommand entityCommand);
    }


    internal class StandAloneOrderHandler:OrderHandler
    {
        internal StandAloneOrderHandler(Game game) : base(game)
        {
        }

        internal override void HandleOrder(IEntityCommand entityCommand)
        {
            entityCommand.ActionCommand(_game);
        }
    }


    internal class ServerOrderHandler:OrderHandler
    {
        bool HasClients = false;


        Dictionary<Guid, List<RemoteConnection>> factionListners = new Dictionary<Guid, List<RemoteConnection>>();

        internal ServerOrderHandler(Game game) : base(game)
        {
        }

        internal override void HandleOrder(IEntityCommand entityCommand)
        {
            RXOrder(entityCommand);
        }

        internal void RXOrder(IEntityCommand entityCommand)
        {
            if(entityCommand.ActionCommand(_game) && HasClients)
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
        internal override void HandleOrder(IEntityCommand entityCommand)
        {
            TXOrder(entityCommand);
        }
        internal void TXOrder(IEntityCommand entityCommand)
        {
            ServerConnection.SendOrder(entityCommand);
        }
        internal void RXOrder(IEntityCommand entityCommand)
        {
            entityCommand.ActionCommand(_game);
        }


    }


    internal class RemoteConnection //flesh this out once I've got lidgren back in
    {
        internal void SendOrder(IEntityCommand entityCommand)
        { }


    }
}
