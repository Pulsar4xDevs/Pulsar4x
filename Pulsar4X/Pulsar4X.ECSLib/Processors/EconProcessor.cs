using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal static class EconProcessor
    {

        private const int _timeBetweenRuns = 68400; //one terran day.

        /// <summary>
        /// Initializes this Processor.
        /// </summary>
        internal static void Initialize()
        {
        }

        internal static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems) //TODO thread this
            {
                system.EconLastTickRun += deltaSeconds;
                if (system.EconLastTickRun >= _timeBetweenRuns)
                {
                    int econTicks = system.EconLastTickRun / _timeBetweenRuns;
                    //should each colony be run in sequence, or each process... does it matter?
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyMinesDB>())
                    {
                        MineProcessor.MineResources(colonyEntity, econTicks);                        
                    }
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyRefiningDB>())
                    {
                        RefiningProcessor.RefineMaterials(colonyEntity, game, econTicks);
                    }
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyConstructionDB>())
                    {
                        ConstructionProcessor.ConstructStuff(colonyEntity, game, econTicks);
                    }
  
                    system.EconLastTickRun -= _timeBetweenRuns * econTicks;
                }
            }
        }

        internal static void ReCalc(Entity entity)
        {


        }
    }
}