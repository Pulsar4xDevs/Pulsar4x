using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    static public class OrderProcessor
    {

        static public void Process(Game game)
        {
            Dictionary<Guid, StarSystem> systems = game.Systems;
            if (game.Settings.EnableMultiThreading ?? false)
            {
                // Process the orderqueue
                Parallel.ForEach(game.Players, player => player.ProcessOrders());
                Parallel.ForEach(systems, system => ProcessSystem(system.Value));
            }
            else
            {
                foreach (var player in game.Players)
                {
                    player.ProcessOrders();
                }

                foreach (var system in systems) //TODO thread this
                {

                    ProcessSystem(system.Value);
                }
            }
        }

        static public void ProcessSystem(StarSystem system)
        {
            foreach (Entity ship in system.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                ProcessShip(ship);
            }
        }

        static public void ProcessShip(Entity ship)
        {
            ShipInfoDB sInfo = ship.GetDataBlob<ShipInfoDB>();

            if (sInfo.Orders.Count == 0)
                return;

            if (sInfo.Orders.Peek().processOrder())
                sInfo.Orders.Dequeue();

            return;
        }
    }
}
