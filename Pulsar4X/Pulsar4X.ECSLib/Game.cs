using System;
using System.Collections.Generic;
using System.Threading;

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

        public EngineComms EngineComms { get; private set; }       

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

        public Game()
        {
            IsLoaded = false;
            GlobalManager = new EntityManager();
            GlobalManager.Clear(true);
            Instance = this;

            StarSystems = new List<StarSystem>();

            CurrentDateTime = DateTime.Now;

            NextSubpulse = new SubpulseLimitRequest {MaxSeconds = 5};

            CurrentInterrupt = new Interrupt();

            EngineComms = new EngineComms();

            // make sure we have defalt galaxy settings:
            GalaxyFactory.InitToDefaultSettings();

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

                // start by checking the default queue, this queue always exisits and is used 
                // for init and important, faction neutral, messages.
                Message message;
                if (EngineComms.LibPeekFactionInQueue(Guid.Empty, out message) && IsMessageValid(message))
                {
                    // we have a valid message we had better take it out of the queue:
                    message = EngineComms.LibReadFactionInQueue(Guid.Empty);

                    // process it:
                    ProcessMessage(null, message, ref quit);
                    messageProcessed = true;
                }

                // loop through all the incoming queues looking for a new message:
                List<Entity> factions = GlobalManager.GetAllEntitiesWithDataBlob<FactionDB>();
                foreach (Entity faction in factions)
                {
                    // lets just take a peek first:
                    if (EngineComms.LibPeekFactionInQueue(faction.Guid, out message) && IsMessageValid(message))
                    {
                        // we have a valid message we had better take it out of the queue:
                        message = EngineComms.LibReadFactionInQueue(faction.Guid);

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

        /// <summary>
        /// Process messages. Note that Faction can be null if the message cam in through the default queue.
        /// </summary>
        private void ProcessMessage(Entity faction, Message message, ref bool quit)
        {
            if (message == null)
            {
                return;
            }

            switch (message.Type)
            {
                case MessageType.Quit:
                    quit = true;                                        // cause the game to quit!
                    break;
                case MessageType.Save:
                    string savePath = message.Data as string;
                    if(string.IsNullOrWhiteSpace(savePath))
                        break;
                    SaveGame saveGame = new SaveGame(savePath);
                    saveGame.Save();
                    EngineComms.FirstOrDefault().OutMessageQueue.Enqueue(new Message(MessageType.GameStatusUpdate, "Saved to " + savePath));
                    break;
                case MessageType.Load:
                    string loadPath = message.Data as string;
                    if(string.IsNullOrWhiteSpace(loadPath))
                        break;
                    SaveGame loadGame = new SaveGame(loadPath);
                    loadGame.Load(loadPath);
                    EngineComms.FirstOrDefault().OutMessageQueue.Enqueue(new Message(MessageType.GameStatusUpdate, "Loaded from " + loadPath));
                    break;
                case MessageType.Echo:
                    if (faction == null)
                        EngineComms.LibWriteOutQueue(Guid.Empty, message);     // echo chamber ;)
                    else
                        EngineComms.LibWriteOutQueue(faction.Guid, message);     // echo chamber ;)
                    break;
                default:
                    throw new System.Exception("Message of type: " + message.Type.ToString() + ", Went unprocessed.");
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
