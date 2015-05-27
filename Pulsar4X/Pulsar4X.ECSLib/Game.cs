using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public delegate void ProcessorFunction(List<StarSystem> systems, int deltaSeconds);

    public class Game
    {
        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        public EntityManager GlobalManager { get; private set; }

        /// <summary>
        /// Singleton Instance of Game
        /// </summary>
        public static Game Instance { get; private set; }

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        public List<StarSystem> StarSystems { get; set; }
        public DateTime CurrentDateTime { get; set; }

        public SubpulseLimitRequest NextSubpulse
        {
            get 
            {
                lock (_subpulseLockObj)
                {
                    return _nextSubpulse;
                }
            }
            set
            {
                lock (_subpulseLockObj)
                {
                    if (_nextSubpulse == null)
                    {
                        _nextSubpulse = value;
                        return;
                    }
                    if (value.MaxSeconds < _nextSubpulse.MaxSeconds)
                    {
                        // Only take the shortest subpulse.
                        _nextSubpulse = value;
                    }
                }
            }    
        }
        private SubpulseLimitRequest _nextSubpulse;
        private readonly object _subpulseLockObj = new object();

        /// <summary>
        /// List of processor functions run when time is advanced.
        /// </summary>
        private static List<ProcessorFunction> _processors;

        public Interrupt CurrentInterrupt { get; set; }

        public event EventHandler PostLoad;

        public bool IsLoaded { get; private set; }

        public string GameName;
        public string SavePath;

        [JsonIgnore]
        public SaveGame SaveGame;

        [JsonIgnore] 
        public List<IServerTransportLayer> Servers;

        [JsonIgnore] 
        private LibProcessLayer _currentComms;

        [JsonIgnore] 
        internal bool QuitMessageReceived;

        [JsonIgnore] 
        private Thread _commsThread;
        

        public Game(LibProcessLayer comms, string gameName, string savePath = null)
        {
            _currentComms = comms;

            GameName = gameName;
            if (savePath == null)
            {
                SavePath = Directory.GetCurrentDirectory() + "//" + GameName;
            }
            else
            {
                SavePath = savePath;
            }

            IsLoaded = false;
            GlobalManager = new EntityManager();
            GlobalManager.Clear(true);

            StarSystems = new List<StarSystem>();

            CurrentDateTime = DateTime.Now;

            NextSubpulse = new SubpulseLimitRequest {MaxSeconds = 5};

            CurrentInterrupt = new Interrupt();

            // make sure we have defalt galaxy settings:
            GalaxyFactory.InitToDefaultSettings();

            // Setup processors.
            InitializeProcessors();

            SaveGame = new SaveGame(SavePath + "//" + GameName + ".json");

            QuitMessageReceived = false;
        }

        /// <summary>
        /// Prepares, and defines the order that processors are run in.
        /// </summary>
        private static void InitializeProcessors()
        {
            OrbitProcessor.Initialize();
            InstallationProcessor.Initialize();
            _processors = new List<ProcessorFunction>
            {
                // Defines the order that processors are run.
                OrbitProcessor.Process,
                InstallationProcessor.Process
            };
        }

        /// <summary>
        /// Runs the game simulation in a loop. Will check for and process messages from the UI.
        /// </summary>
        public void MainGameLoop()
        {
            _commsThread = Thread.CurrentThread;

            while (!QuitMessageReceived)
            {
                if (!_currentComms.ProcessMessages())
                {
                    // we don't have waiting messages. 
                    // we should probably wait for a while for new stuff to queue up
                    Thread.Sleep(5);
                }
            }
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

                // Execute all processors. Magic happens here.
                foreach (ProcessorFunction processor in _processors)
                {
                    processor(StarSystems, deltaSeconds);
                }

                // Update our remaining values.
                deltaSeconds -= subpulseTime;
                timeAdvanced += subpulseTime;
            }

            if (CurrentInterrupt.StopProcessing)
            {
                // Notify the user?
                // Gamelog?
                // todo: review interrupt messages.
            }
            return timeAdvanced;
        }


        internal void PostGameLoad(DateTime currentDateTime, EntityManager globalManager, List<StarSystem> starSystems)
        {
            CurrentDateTime = currentDateTime;
            GlobalManager = globalManager;
            StarSystems = starSystems;

            // Invoke the Post Load event:
            if (PostLoad != null)
                PostLoad(this, EventArgs.Empty);

            // Post load event completed. Drop all handlers.
            PostLoad = null;

            // set isLoaded to true:
            IsLoaded = true;
        }
    }
}
