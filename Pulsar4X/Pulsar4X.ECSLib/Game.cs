using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.Helpers;
using Pulsar4X;
using System.Threading;
using Pulsar4X.ECSLib.Processors;

namespace Pulsar4X.ECSLib
{
    public class Game
    {
        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        public EntityManager GlobalManager { get { return m_globalManager; } }
        private EntityManager m_globalManager;

        /// <summary>
        /// Singleton Instance of Game
        /// </summary>
        public static Game Instance { get { return m_instance; } }
        private static Game m_instance;

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        public List<StarSystem> StarSystems { get; set; }
        public DateTime CurrentDateTime { get; set; }

        public Engine_Comms EngineComms { get; private set; }       

        public SubpulseLimitRequest NextSubpulse
        {
            get 
            {
                lock (subpulse_lockObj)
                {
                    return m_nextSubpulse;
                }
            }
            set
            {
                lock (subpulse_lockObj)
                {
                    if (m_nextSubpulse == null)
                    {
                        m_nextSubpulse = value;
                        return;
                    }
                    if (value.MaxSeconds < m_nextSubpulse.MaxSeconds)
                    {
                        // Only take the shortest subpulse.
                        m_nextSubpulse = value;
                    }
                }
            }    
        }
        private SubpulseLimitRequest m_nextSubpulse;
        private object subpulse_lockObj = new object();

        public Interrupt CurrentInterrupt { get; set; }

        public Game()
        {
            m_globalManager = new EntityManager();
            m_instance = this;

            StarSystems = new List<StarSystem>();

            CurrentDateTime = DateTime.Now;

            NextSubpulse = new SubpulseLimitRequest();
            NextSubpulse.MaxSeconds = 5;

            CurrentInterrupt = new Interrupt();

            EngineComms = new Engine_Comms();

            // Setup time Phases.
            PhaseProcessor.Initialize();
        }

        /// <summary>
        /// Time advancement code. Attempts to advance time by the number of seconds
        /// passed to it.
        /// 
        /// Interrupts may prevent the entire requested timeframe from being advanced.
        /// </summary>
        /// <param name="deltaSeconds">Time Advance Requested</param>
        /// <returns>Total Time Advanced</returns>
        public int AdvanceTime(int deltaSeconds)
        {
            int timeAdvanced = 0;

            // Clamp deltaSeconds to a multiple of our MinimumTimestep.
            deltaSeconds = deltaSeconds - (deltaSeconds % GameSettings.GameConstants.MinimumTimestep);
            if (deltaSeconds == 0)
            {
                deltaSeconds = GameSettings.GameConstants.MinimumTimestep;
            }

            // Clear any interrupt flag before starting the pulse.
            CurrentInterrupt.StopProcessing = false;

            while (!CurrentInterrupt.StopProcessing && deltaSeconds > 0)
            {
                int subpulseTime = Math.Min(NextSubpulse.MaxSeconds, deltaSeconds);
                // Set next subpulse to max value. If it needs to be shortened, it will
                // be shortened in the pulse execution.
                NextSubpulse.MaxSeconds = int.MaxValue;

                // Update our date.
                CurrentDateTime += TimeSpan.FromSeconds(subpulseTime);

                // Execute subpulse phases. Magic happens here.
                PhaseProcessor.Process(subpulseTime);

                // Update our remaining values.
                deltaSeconds -= subpulseTime;
                timeAdvanced += subpulseTime;
            }

            if (CurrentInterrupt.StopProcessing)
            {
                // Notify the user?
                // Gamelog?
                // <@ todo: review interrupt messages.
            }
            return timeAdvanced;
        }

        /// <summary>
        /// Test function to demonstrate the usage of the EntityManager.
        /// </summary>
        public void EntityManagerTests()
        {
            // Create an entity with individual DataBlobs.
            int planet = GlobalManager.CreateEntity();
            GlobalManager.SetDataBlob(planet, OrbitDB.FromStationary(5));
            GlobalManager.SetDataBlob(planet, new PopulationDB(10));

            // Create an entity with a DataBlobList.
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(OrbitDB.FromStationary(2));
            GlobalManager.CreateEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs.Add(new PopulationDB(9));
            GlobalManager.CreateEntity(dataBlobs);

            // Get all DataBlobs of a specific type.
            List<PopulationDB> populations = GlobalManager.GetAllDataBlobsOfType<PopulationDB>();
            List<OrbitDB> orbits = GlobalManager.GetAllDataBlobsOfType<OrbitDB>();

            // Get all DataBlobs of a specific entity.
            dataBlobs = GlobalManager.GetAllDataBlobsOfEntity(planet);

            // Remove an entity.
            GlobalManager.RemoveEntity(planet);

            // Add a new entity (using a list of DataBlobs.
            GlobalManager.CreateEntity(dataBlobs);

            // Find all entities with a specific DataBlob.
            List<int> populatedEntities = GlobalManager.GetAllEntitiesWithDataBlob<PopulationDB>();

            // Get the Population DB of a specific entity.
            PopulationDB planetPopDB = GlobalManager.GetDataBlob<PopulationDB>(planet);

            // Change the planet Pop.
            planetPopDB.PopulationSize += 5;

            // Get the current value.
            PopulationDB planetPopDB2 = GlobalManager.GetDataBlob<PopulationDB>(planet);

            if (planetPopDB.PopulationSize != planetPopDB2.PopulationSize)
            {
                // Note, we wont hit this because the value DID change.
                throw new InvalidOperationException();
            }

            // Forget it, remove the DataBlob.
            GlobalManager.RemoveDataBlob<PopulationDB>(planet);

            if (GlobalManager.GetDataBlob<PopulationDB>(1) == null)
            {
                // Will hit this!
                // Entity 1 doesn't have a population, so GetDataBlob returns null.

                // This crap is so you can reliably breakpoint this (even in release mode) without it being optimized away.
                int i = 0;
                i++;
            }
            else
            {
                // Wont hit this!
                // Since there's no pop!
                throw new InvalidOperationException();
            }

            // Remove the crap we added.
            GlobalManager.RemoveEntity(planet);
            GlobalManager.RemoveEntity(planet + 1);
            GlobalManager.RemoveEntity(planet + 2);
        }
    }
}
