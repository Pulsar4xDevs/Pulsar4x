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
                Parallel.ForEach(systems, system => ProcessSystem(system.Value, game));
            }
            else
            {
                foreach (var player in game.Players)
                {
                    player.ProcessOrders();
                }

                foreach (var system in systems) //TODO thread this
                {

                    ProcessSystem(system.Value, game);
                }
            }
        }

        static private void ProcessSystem(StarSystem system, Game game)
        {
            foreach (Entity ship in system.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                if (ship.GetDataBlob<ShipInfoDB>().NumOrders() == 0)
                    continue;
                    
                if (ship.GetDataBlob<ShipInfoDB>().CheckNextOrder().processOrder())
                    ship.GetDataBlob<ShipInfoDB>().RemoveNextOrder();
            }
        }
    }
}
