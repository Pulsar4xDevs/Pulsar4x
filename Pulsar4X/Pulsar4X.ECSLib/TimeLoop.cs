using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public delegate void DateChangedEventHandler(DateTime newDate);

    [JsonObject(MemberSerialization.OptIn)]
    public class TimeLoop
    {
        #region Fields
        private readonly Game _game;

        /// <summary>
        /// Stopwatch to time how long DoProcessing takes to complete.
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// Timer used to execute DoProcessing
        /// </summary>
        private readonly Timer _timer = new Timer();

        [JsonProperty]
        private SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>> _entityDictionary = new SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>>();

        [JsonProperty]
        private DateTime _gameGlobalDateTime;

        /// <summary>
        /// Tracks if DoProcessing should recurse after finishing.
        /// </summary>
        private bool _isOvertime;

        /// <summary>
        /// Tracks if DoProcessing is currently running.
        /// </summary>
        private bool _isProcessing;

        private TimeSpan _tickInterval = TimeSpan.FromMilliseconds(250);
        private float _timeMultiplier = 1f;

        internal DateTime TargetDateTime;
        #endregion

        #region Properties
        /// <summary>
        /// Multiplier applied to the Timer interval.
        /// </summary>
        public float TimeMultiplier
        {
            get { return _timeMultiplier; }
            set
            {
                _timeMultiplier = value;
                _timer.Interval = _tickInterval.TotalMilliseconds * value;
            }
        }

        /// <summary>
        /// Timer interval (not including TimeMultiplier)
        /// </summary>
        public TimeSpan TickFrequency
        {
            get { return _tickInterval; }
            set
            {
                _tickInterval = value;
                _timer.Interval = _tickInterval.TotalMilliseconds * _timeMultiplier;
            }
        }

        /// <summary>
        /// Length of one tick. Currently set to 5 minutes.
        /// </summary>
        public TimeSpan Ticklength { get; set; } = TimeSpan.FromSeconds(3600);

        /// <summary>
        /// length of time it took to process the last DoProcess
        /// </summary>
        public TimeSpan LastProcessingTime { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// Current Date in-game
        /// </summary>
        public DateTime GameGlobalDateTime
        {
            get { return _gameGlobalDateTime; }
            internal set
            {
                _gameGlobalDateTime = value;
                if (_game.SyncContext != null)
                {
                    _game.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread, so the event is invoked on that thread.
                }
                else
                {
                    InvokeDateChange(value); //if context is null, we're probibly running tests or headless. in this case we're not going to marshal this.    
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when the game date is incremented.
        /// All systems are in sync at this event.
        /// </summary>
        public event DateChangedEventHandler GameGlobalDateChangedEvent;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game"></param>
        internal TimeLoop(Game game)
        {
            _game = game;
            _timer.Interval = _tickInterval.TotalMilliseconds;
            _timer.Enabled = false;
            _timer.Elapsed += Timer_Elapsed;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// This invokes the DateChangedEvent.
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {
            var logevent = new Event(GameGlobalDateTime, "Game Global Date Changed");
            logevent.EventType = EventType.GlobalDateChange;
            _game.EventLog.AddEvent(logevent);

            GameGlobalDateChangedEvent?.Invoke(GameGlobalDateTime);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isProcessing)
            {
                DoProcessing(); //run DoProcessing if we're not already processing
            }
            else
            {
                _isOvertime = true; //if we're processing, then processing it taking longer than the sim speed
            }
        }

        private void DoProcessing()
        {
            _isProcessing = true;
            _timer.Stop();
            _timer.Start(); //reset timer
            _stopwatch.Start(); //start the processor loop stopwatch
            _isOvertime = false;

            //check for global interupts
            TargetDateTime = GameGlobalDateTime + Ticklength;


            while (GameGlobalDateTime < TargetDateTime)
            {
                DateTime nextInterupt = ProcessNextInterupt(TargetDateTime);
                //do system processors

                if (_game.Settings.EnableMultiThreading == true) //threaded
                {
                    Parallel.ForEach(_game.Systems.Values, starSys => starSys.SystemManager.ManagerSubpulses.ProcessSystem(nextInterupt));
                }
                //The above 'blocks' till all the tasks are done.
                else //non threaded
                {
                    foreach (StarSystem starSys in _game.Systems.Values)
                    {
                        starSys.SystemManager.ManagerSubpulses.ProcessSystem(nextInterupt);
                    }
                }

                GameGlobalDateTime = nextInterupt; //set the GlobalDateTime this will invoke the datechange event.
            }

            LastProcessingTime = _stopwatch.Elapsed; //how long the processing took
            _stopwatch.Reset();

            if (_isOvertime)
            {
                // TODO: This will cause a StackOverflow on slow computers with large processing.
                DoProcessing(); //if running overtime, DoProcessing wont be triggered by the event, so trigger it here.
            }
            _isProcessing = false;
        }

        private DateTime ProcessNextInterupt(DateTime maxDateTime)
        {
            DateTime processedTo;
            DateTime nextInteruptDateTime;
            if (_entityDictionary.Keys.Count != 0)
            {
                nextInteruptDateTime = _entityDictionary.Keys.Min();
                if (nextInteruptDateTime <= maxDateTime)
                {
                    foreach (KeyValuePair<PulseActionEnum, List<SystemEntityJumpPair>> delegateListPair in _entityDictionary[nextInteruptDateTime])
                    {
                        foreach (SystemEntityJumpPair jumpPair in delegateListPair.Value) //foreach entity in the value list
                        {
                            //delegateListPair.Key.DynamicInvoke(_game, jumpPair);
                            PulseActionDictionary.DoAction(delegateListPair.Key, _game, jumpPair);
                        }
                    }
                    processedTo = nextInteruptDateTime;
                }
                else
                {
                    processedTo = maxDateTime;
                }
            }
            else
            {
                processedTo = maxDateTime;
            }

            return processedTo;
        }
        #endregion


        /// <summary>
        /// Adds an interupt where systems are interacting (ie an entity jumping between systems)
        /// this forces all systems to synch at this datetime.
        /// </summary>
        /// <param name="datetime"></param>
        /// <param name="action"></param>
        /// <param name="jumpPair"></param>
        internal void AddSystemInteractionInterupt(DateTime datetime, PulseActionEnum action, SystemEntityJumpPair jumpPair)
        {
            if (!_entityDictionary.ContainsKey(datetime))
            {
                _entityDictionary.Add(datetime, new Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>());
            }
            if (!_entityDictionary[datetime].ContainsKey(action))
            {
                _entityDictionary[datetime].Add(action, new List<SystemEntityJumpPair>());
            }
            _entityDictionary[datetime][action].Add(jumpPair);
        }

        internal void AddHaltingInterupt(DateTime datetime) { throw new NotImplementedException(); }

        #region Public Time Methods. UI interacts with time here
        /// <summary>
        /// Pauses the timeloop
        /// </summary>
        public void PauseTime()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Starts the timeloop
        /// </summary>
        public void StartTime()
        {
            _timer.Start();
        }


        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep()
        {
            DoProcessing();
            _timer.Stop();
        }
        #endregion
    }
}