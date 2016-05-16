using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    internal class EconProcessor
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
            TechProcessor.ProcessSystem(system, game);

            foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyMinesDB>())
            {
                MineProcessor.MineResources(colonyEntity);
            }
            foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyRefiningDB>())
            {
                RefiningProcessor.RefineMaterials(colonyEntity, game);
            }
            foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyConstructionDB>())
            {
                ConstructionProcessor.ConstructStuff(colonyEntity, game);
            }
            foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            {
                PopulationProcessor.GrowPopulation(colonyEntity);
            }
        }
    }
}