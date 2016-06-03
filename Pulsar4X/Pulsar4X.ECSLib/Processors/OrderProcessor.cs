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
                Parallel.ForEach(game.Players, player => Process)
                Parallel.ForEach(systems, system => ProcessSystem(system, game));
            }
            else
            {
                foreach (var system in systems) //TODO thread this
                {
                    ProcessSystem(system, game);
                }
            }
        }

        private void ProcessSystem(StarSystem system, Game game)
        {

        }
}
