using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Pulsar4X.ECSLib
{
    public delegate void DateChangedEventHandler(DateTime newDate);
    [JsonObject(MemberSerialization.OptIn)]
    public class TimeLoop
    {
        [JsonProperty]
        private SortedDictionary<DateTime, Dictionary<SystemActionEnum, List<SystemEntityJumpPair>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<SystemActionEnum, List<SystemEntityJumpPair>>>();

        private Stopwatch _stopwatch = new Stopwatch();
        private Timer _timer = new Timer();

        //changes how often the tick happens
        public float TimeMultiplier
        {
            get {return _timeMultiplier;}
            set
            {
                _timeMultiplier = value;
                _timer.Interval = _tickInterval.TotalMilliseconds * value;
            }
        } 
        private float _timeMultiplier = 1f;

        private TimeSpan _tickInterval = TimeSpan.FromSeconds(1);
        public TimeSpan TickFrequency { get { return _tickInterval; } set { _tickInterval = value;
            _timer.Interval = _tickInterval.TotalMilliseconds * _timeMultiplier;
        } }


        public TimeSpan Ticklength { get; set; } = TimeSpan.FromSeconds(1);

        private bool _isProcessing = false;
        private bool _isOvertime = false;
        private Game _game;
        /// <summary>
        /// length of time it took to process the last DoProcess
        /// </summary>
        public TimeSpan LastProcessingTime { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// This invokes the DateChangedEvent.
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {
            GameGlobalDateChangedEvent?.Invoke(GameGlobalDateTime);
        }
        [JsonProperty]
        private DateTime _gameGlobalDateTime;
        internal DateTime _targetDateTime;
        public DateTime GameGlobalDateTime
        {
            get { return _gameGlobalDateTime; }
            internal set
            {
                _gameGlobalDateTime = value;
                if (_game.SyncContext != null)
                    _game.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread, so the event is invoked on that thread.
                else
                    InvokeDateChange(value);//if context is null, we're probibly running tests or headless. in this case we're not going to marshal this.    
            }
        }
        /// <summary>
        /// Fired when the game date is incremented. 
        /// All systems are in sync at this event.
        /// </summary>
        public event DateChangedEventHandler GameGlobalDateChangedEvent;

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

        #region Public Time Methods. UI interacts with time here

        /// <summary>
        /// PausesTimeloop
        /// </summary>
        public void PauseTime()
        {
            _timer.Stop();
        }
        /// <summary>
        /// Stars the timeloop
        /// </summary>
        public void StartTime()
        {
            _timer.Start();
        }

        #endregion


        /// <summary>
        /// Adds an interupt where systems are interacting (ie an entity jumping between systems)
        /// this forces all systems to synch at this datetime.
        /// </summary>
        /// <param name="datetime"></param>
        /// <param name="action"></param>
        /// <param name="jumpPair"></param>
        internal void AddSystemInteractionInterupt(DateTime datetime, SystemActionEnum action, SystemEntityJumpPair jumpPair)
        {
            if (!EntityDictionary.ContainsKey(datetime))
                EntityDictionary.Add(datetime, new Dictionary<SystemActionEnum, List<SystemEntityJumpPair>>());
            if (!EntityDictionary[datetime].ContainsKey(action))
                EntityDictionary[datetime].Add(action, new List<SystemEntityJumpPair>());
            EntityDictionary[datetime][action].Add(jumpPair);
        }

        internal void AddHaltingInterupt(DateTime datetime)
        {
            throw new NotImplementedException();
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
            _targetDateTime = GameGlobalDateTime + Ticklength;

         
            while (GameGlobalDateTime < _targetDateTime)
            {
                DateTime nextInterupt = ProcessNextInterupt(_targetDateTime);
                //do system processors
                Parallel.ForEach<StarSystem>(_game.Systems.Values, item => ProcessSystem(item, nextInterupt));
                //The above 'blocks' till all the tasks are done.

                GameGlobalDateTime = nextInterupt; //set the GlobalDateTime this will invoke the datechange event.
            }

            LastProcessingTime = _stopwatch.Elapsed; //how long the processing took
            _stopwatch.Reset();

            if (_isOvertime)
            {
                DoProcessing(); //if running overtime, DoProcessing wont be triggered by the event, so trigger it here.
            }
            _isProcessing = false;
        }

        private DateTime ProcessNextInterupt(DateTime maxDateTime)
        {
            DateTime processedTo;
            DateTime nextInteruptDateTime;
            if (EntityDictionary.Keys.Count != 0)
            {
                nextInteruptDateTime = EntityDictionary.Keys.Min();
                if (nextInteruptDateTime <= maxDateTime)
                {
                    foreach (var delegateListPair in EntityDictionary[nextInteruptDateTime])
                    {
                        foreach (var jumpPair in delegateListPair.Value) //foreach entity in the value list
                        {
                            //delegateListPair.Key.DynamicInvoke(_game, jumpPair);
                            ActionDelegateDictionary.DoAction(delegateListPair.Key, _game, jumpPair);
                        }

                    }
                    processedTo = nextInteruptDateTime;
                }
                else
                    processedTo = maxDateTime;
            }
            else
                processedTo = maxDateTime;

            return processedTo;
        }

        private void ProcessSystem(object systemObj, DateTime toDateTime)
        {
            //check validity of commands etc. here.


            StarSystem system = systemObj as StarSystem;            

            //TimeSpan systemElapsedTime = new TimeSpan();
            DateTime systemTime = system.SystemSubpulses.SystemLocalDateTime;
            //the system may need to run several times for a wanted tickLength
            //keep processing the system till we've reached the wanted ticklength
            while (systemTime < toDateTime)
            {

                //calculate max time the system can run/time to next interupt
                TimeSpan timeDelta = TimeSpan.FromSeconds( Math.Min(Ticklength.TotalSeconds, (toDateTime - systemTime).TotalSeconds)); 
                ShipMovementProcessor.Process(system, timeDelta.Seconds);
      
                //this should handle predicted events, ie econ, production, shipjumps, sensors etc.
                systemTime = system.SystemSubpulses.ProcessNextInterupt(timeDelta);

            }
        }
    }


    /// <summary>
    /// handles and processes entities for a specific datetime. 
    /// TODO:  handle removal of entities from the system.
    /// TODO:  handle removal of ability datablobs from an entity
    /// TODO:  handle passing an entity from this system to another, and carry it's subpulses/interupts across. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SystemSubPulses
    {
        //TODO there may be a more efficent datatype for this. 
        [JsonProperty]
        public SortedDictionary<DateTime, Dictionary<SystemActionEnum, List<Entity>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<SystemActionEnum, List<Entity>>>();


        private StarSystem _starSystem;
        /// <summary>
        /// Fires when the system date is updated, 
        /// Any entitys that have move (though not neccicarly orbits) will have updated
        /// other systems may not be in sync on this event. 
        /// </summary>
        public event DateChangedEventHandler SystemDateChangedEvent;
        /// <summary>
        /// Invoke the SystemDateChangedEvent
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {            
            SystemDateChangedEvent?.Invoke(SystemLocalDateTime);
        }

        [JsonProperty]
        private DateTime _systemLocalDateTime;
        public DateTime SystemLocalDateTime
        {
            get { return _systemLocalDateTime; }
            private set
            {
                _systemLocalDateTime = value;
                if (_starSystem.Game.SyncContext != null)
                    _starSystem.Game.SyncContext.Post(InvokeDateChange, value);//marshal to the main (UI) thread, so the event is invoked on that thread.
                else //if context is null, we're probibly running tests or headless.
                    InvokeDateChange(value); //in this case we're not going to marshal this. (event will fire on *THIS* thread)   
            }
        }
        
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="parentStarSystem"></param>
        internal SystemSubPulses(StarSystem parentStarSystem)
        {
            _starSystem = parentStarSystem;
            _systemLocalDateTime = parentStarSystem.Game.CurrentDateTime;

            //can add either by creating and passing an Action
            //Action<StarSystem> economyMethod = EconProcessor.ProcessSystem;
            //AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.EconomyCycleTime, economyMethod);
            //or can add it by passing the method
            //AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.OrbitCycleTime, OrbitProcessor.UpdateSystemOrbits);
            AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.EconomyCycleTime, SystemActionEnum.EconProcessor);
            AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.OrbitCycleTime, SystemActionEnum.OrbitProcessor);
        }


        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime 
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, SystemActionEnum action, Entity entity)
        {
            if (!EntityDictionary.ContainsKey(nextDateTime))
                EntityDictionary.Add(nextDateTime, new Dictionary<SystemActionEnum, List<Entity>>());
            if (!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, new List<Entity>());
            EntityDictionary[nextDateTime][action].Add(entity);
        }


        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, SystemActionEnum action)
        {
            if (!EntityDictionary.ContainsKey(nextDateTime))
                EntityDictionary.Add(nextDateTime, new Dictionary<SystemActionEnum, List<Entity>>());
            if (!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, null);
        }

        /// <summary>
        /// process to next subpulse
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="maxSpan">maximum time delta</param>
        /// <returns>datetime processed to</returns>
        internal DateTime ProcessNextInterupt(TimeSpan maxSpan)
        {
            DateTime nextInteruptDateTime;
            if (EntityDictionary.Keys.Count != 0)
            {
                nextInteruptDateTime = EntityDictionary.Keys.Min();
                if (nextInteruptDateTime <= SystemLocalDateTime + maxSpan)
                {
                    foreach (KeyValuePair<SystemActionEnum, List<Entity>> delegateListPair in EntityDictionary[nextInteruptDateTime])
                    {
                        if (delegateListPair.Value == null) //if the list is null, it's a systemwide interupt
                        {
                            //delegateListPair.Key.DynamicInvoke(_starSystem);
                            ActionDelegateDictionary.DoAction(delegateListPair.Key, _starSystem);
                        }
                        else
                            foreach (Entity entity in delegateListPair.Value) //foreach entity in the value list
                            {
                                //delegateListPair.Key.DynamicInvoke(entity);
                                ActionDelegateDictionary.DoAction(delegateListPair.Key, _starSystem, entity);
                            }
                    }

                    SystemLocalDateTime = nextInteruptDateTime;
                    EntityDictionary.Remove(nextInteruptDateTime);
                }
                else
                    SystemLocalDateTime  += maxSpan;
            }
            else
                SystemLocalDateTime  += maxSpan;

            return SystemLocalDateTime;
        }
    }
}
