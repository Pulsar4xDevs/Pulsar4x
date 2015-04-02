using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X;
using System.Threading;
using Pulsar4X.ECSLib.Helpers;
using Pulsar4X.ECSLib.Processors;

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

        public Engine_Comms EngineComms { get; private set; }       

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

        public Game()
        {
            GlobalManager = new EntityManager();
            Instance = this;

            StarSystems = new List<StarSystem>();

            CurrentDateTime = DateTime.Now;

            NextSubpulse = new SubpulseLimitRequest {MaxSeconds = 5};

            CurrentInterrupt = new Interrupt();

            EngineComms = new Engine_Comms();

            // Setup processors.
            InitializeProcessors();
        }

        /// <summary>
        /// Prepares, and defines the order that processors are run in.
        /// </summary>
        private static void InitializeProcessors()
        {
            OrbitProcessor.Initialize();

            _processors = new List<ProcessorFunction>
            {
                // Defines the order that processors are run.
                OrbitProcessor.Process
            };
        }

        /// <summary>
        /// Runs the game simulation in a loop. Will check for and process messages from the UI.
        /// </summary>
        public void MainGameLoop()
        {
            bool quit = false;
            bool messageProcessed = false;

            while (!quit)
            {
                // lets first check if there are things waiting in a queue:
                if (EngineComms.LibMessagesWaiting() == false)
                {
                    // there is nothing from the UI, so lets sleep for a while before checking again.
                    Wait();
                    continue; // go back and check again.
                }

                // loop through all the incoming queues looking for a new message:
                List<int> factions = GlobalManager.GetAllEntitiesWithDataBlob<PopulationDB>();
                foreach (int faction in factions)
                {
                    // lets just take a peek first:
                    Message message;
                    if (EngineComms.LibPeekFactionInQueue(faction, out message) && IsMessageValid(message))
                    {
                        // we have a valid message we had better take it out of the queue:
                        message = EngineComms.LibReadFactionInQueue(faction);

                        // process it:
                        ProcessMessage(faction, message, ref quit);
                        messageProcessed = true;
                    }
                }

                // lets check if we processed a valid message this time around:
                if (messageProcessed)
                {
                    // so we processed a valid message, better check for a new one right away:
                    messageProcessed = false;
                }
                else
                {
                    // we didn't process a valid message... 
                    // we should probably wait for a while for the pulse to finish or new stuff to queue up
                    Wait();
                }
            }
        }

        private bool IsMessageValid(Message message)
        {
            return true; // we will do this until we have messages that can be invalid!!
        }

        private void ProcessMessage(int faction, Message message, ref bool quit)
        {
            if (message == null)
            {
                return;
            }

            switch (message._messageType)
            {
                case Message.MessageType.Quit:
                    quit = true;                                        // cause the game to quit!
                    break;
                case Message.MessageType.Echo:
                    EngineComms.LibWriteOutQueue(faction, message);     // echo chamber ;)
                    break;
                default:
                    throw new System.Exception("Message of type: " + message._messageType.ToString() + ", Went unprocessed.");
            }
        }

        private void Wait()
        {
            // we should have a better way of doing this
            // is there a way for the EnginComs class to fire an event to wake the thread when 
            // a new message come is??
            // that would be the ideal way to do it, no wasted time, no wasted CPU usage.
            Thread.Sleep(5);  
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
                // <@ todo: review interrupt messages.
            }
            return timeAdvanced;
        }


        internal void PostLoad(DateTime currentDateTime, EntityManager globalManager, List<StarSystem> starSystems)
        {
            CurrentDateTime = currentDateTime;
            GlobalManager = globalManager;
            StarSystems = starSystems;

            ///< @todo go throuhg all datablobs and call their postLoad functions if they have them.
        }
    }
}
