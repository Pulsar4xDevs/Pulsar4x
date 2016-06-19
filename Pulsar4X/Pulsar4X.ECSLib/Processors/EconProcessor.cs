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
                Parallel.ForEach(systems, system => ProcessSystem(system));
            }
            else
            {
                foreach (var system in systems) //TODO thread this
                {
                    ProcessSystem(system);
                }
            }
        }

        internal static void ProcessSystem(StarSystem starSystem)
        {
            Game game = starSystem.Game;
            //Action<StarSystem> economyMethod = ProcessSystem;
            //system.SystemSubpulses.AddSystemInterupt(system.Game.CurrentDateTime + system.Game.Settings.EconomyCycleTime, economyMethod);
            starSystem.SystemSubpulses.AddSystemInterupt(starSystem.Game.CurrentDateTime + starSystem.Game.Settings.EconomyCycleTime, PulseActionEnum.EconProcessor);


            TechProcessor.ProcessSystem(starSystem, game);

            foreach (Entity colonyEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<ColonyMinesDB>())
            {
                MineProcessor.MineResources(colonyEntity);
            }
            foreach (Entity colonyEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<ColonyRefiningDB>())
            {
                RefiningProcessor.RefineMaterials(colonyEntity, game);
            }
            foreach (Entity colonyEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<ColonyConstructionDB>())
            {
                ConstructionProcessor.ConstructStuff(colonyEntity, game);
            }
            foreach (Entity colonyEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            {
                PopulationProcessor.GrowPopulation(colonyEntity);
            }
        }
    }
}