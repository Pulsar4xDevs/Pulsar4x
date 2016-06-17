using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    internal class OrderProcessor
    {
        [JsonProperty]
        private DateTime _lastRun = DateTime.MinValue;

        internal void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            if (game.CurrentDateTime - _lastRun < game.Settings.EconomyCycleTime)
            {
                return;
            }



            _lastRun = game.CurrentDateTime;

            if (game.Settings.EnableMultiThreading ?? false)
            {
                // Process the orderqueue
                Parallel.ForEach(game.Players, player => player.ProcessOrders());
                Parallel.ForEach(systems, system => ProcessSystem(system, game));
            }
            else
            {
                foreach (var player in game.Players)
                {
                    player.ProcessOrders();
                }

                foreach (var system in systems) //TODO thread this
                {

                    ProcessSystem(system, game);
                }
            }
        }

        private void ProcessSystem(StarSystem system, Game game)
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
