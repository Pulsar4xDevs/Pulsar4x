using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Timers.Timer;

namespace Pulsar4X.ECSLib
{
    public delegate void DateChangedEventHandler(DateTime newDate);
    public class TimeLoop
    {
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
        private DateTime _gameGlobalDateTime;
        public DateTime GameGlobalDateTime
        {
            get { return _gameGlobalDateTime; }
            internal set
            {
                _gameGlobalDateTime = value;
                _game.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread.
            }
        }
        public event DateChangedEventHandler GameGlobalDateChangedEvent;



        public TimeLoop(Game game)
        {
            
            _game = game;
            _timer.Interval = _tickInterval.TotalMilliseconds;
            _timer.Enabled = false;
            _timer.Elapsed += Timer_Elapsed;
            
        }


        public void PauseTime()
        {
            _timer.Stop();
        }

        public void StartTime()
        {
            _timer.Start();
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

        void DoProcessing()
        {
            _isProcessing = true;
            _timer.Stop();
            _timer.Start(); //reset timer
            _stopwatch.Start(); //start the processor loop stopwatch
            _isOvertime = false;
            GameGlobalDateTime += Ticklength; //TODO: move this to the end of processing. however to do this we will need to fix the orbit processor to take a date or timespan. 
            //do system processors
            Parallel.ForEach<StarSystem>(_game.Systems.Values, item => SystemProcessing(item));
            //The above 'blocks' till all the tasks are done.

            LastProcessingTime = _stopwatch.Elapsed;
            _stopwatch.Reset();
            if (_isOvertime)
            {
                DoProcessing(); //if running overtime, DoProcessing wont be triggered by the event, so trigger it here.
            }
            _isProcessing = false;
        }

        void SystemProcessing(object systemObj)
        {
            //check validity of commands etc. here.

            //do any system to system interaction here, ie ship jumping between systems.

            StarSystem system = systemObj as StarSystem;
            //DateTime currentDateTime = system.Game.CurrentDateTime;
            

            //TimeSpan systemElapsedTime = new TimeSpan();
            DateTime systemTime = system.SystemSubpulses.SystemLocalDateTime;
            //the system may need to run several times for a wanted tickLength
            //keep processing the system till we've reached the wanted ticklength
            while (systemTime < GameGlobalDateTime)
            {

                //calculate max time the system can run/time to next interupt
                TimeSpan timeDelta = TimeSpan.FromSeconds( Math.Min(Ticklength.TotalSeconds, (GameGlobalDateTime - systemTime).TotalSeconds)); 
                ShipMovementProcessor.Process(system, timeDelta.Seconds);
                int orbits = 0;
                OrbitProcessor.UpdateSystemOrbits(system, _game, ref orbits);

                //this should handle predicted events, ie econ, production, shipjumps, sensors etc.
                systemTime = system.SystemSubpulses.ProcessNextDateTime(timeDelta);

            }
        }
    }

    
    /// <summary>
    /// handles and processes entities for a specific datetime. 
    /// TODO:  handle removal of entities from the system.
    /// TODO:  handle removal of ability datablobs from an entity
    /// TODO:  handle passing an entity from this system to another, and carry it's subpulses/interupts across. 
    /// </summary>
    public class SystemSubPulses
    {
        //TODO there may be a more efficent datatype for this. 
        private SortedDictionary<DateTime, Dictionary<Delegate, List<Entity>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<Delegate, List<Entity>>>();
        private StarSystem _starSystem;
        //_starSystem.Game.SyncContext;


        public event DateChangedEventHandler SystemDateChangedEvent;
        /// <summary>
        /// Invoke the SystemDateChangedEvent
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {            
            SystemDateChangedEvent?.Invoke(SystemLocalDateTime);
        }
        
        private DateTime _systemLocalDateTime;
        public DateTime SystemLocalDateTime
        {
            get { return _systemLocalDateTime; }
            private set
            {
                _systemLocalDateTime = value;
                _starSystem.Game.SyncContext.Post(InvokeDateChange, value);//marshal to the UI thread          
            }
        }
        

        internal SystemSubPulses(StarSystem parentStarSystem)
        {
            _starSystem = parentStarSystem;
            _systemLocalDateTime = parentStarSystem.Game.CurrentDateTime;
            Action<StarSystem> economyMethod = EconProcessor.ProcessSystem;
            AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.EconomyCycleTime, economyMethod);
        }


        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime 
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, Delegate action, Entity entity)
        {
            if (!EntityDictionary.ContainsKey(nextDateTime))                 
                EntityDictionary.Add(nextDateTime, new Dictionary<Delegate,List<Entity>>());
            if(!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, new List<Entity>());
            EntityDictionary[nextDateTime][action].Add(entity);
        }

        internal void AddSystemInterupt(DateTime nextDateTime, Delegate action)
        {
            if(!EntityDictionary.ContainsKey(nextDateTime))
                EntityDictionary.Add(nextDateTime, new Dictionary<Delegate, List<Entity>>());
            if(!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, null); //a null entity list indicates a systemwide interupt. 
        }

        /// <summary>
        /// process to next subpulse
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="maxSpan">maximum time delta</param>
        /// <returns>datetime processed to</returns>
        internal DateTime ProcessNextDateTime(TimeSpan maxSpan)
        {
            DateTime firstDateTime;
            if (EntityDictionary.Keys.Count != 0)
            {
                firstDateTime = EntityDictionary.Keys.Min();
                if (firstDateTime <= SystemLocalDateTime + maxSpan)
                {
                    foreach (KeyValuePair<Delegate, List<Entity>> delegateListPair in EntityDictionary[firstDateTime])
                    {
                        if (delegateListPair.Value == null) //if the list is null, it's a systemwide interupt
                        {
                            delegateListPair.Key.DynamicInvoke(_starSystem);
                        }
                        else
                            foreach (Entity entity in delegateListPair.Value) //foreach entity in the value list
                            {
                                delegateListPair.Key.DynamicInvoke(entity);
                            }
                    }

                    SystemLocalDateTime = firstDateTime;
                    EntityDictionary.Remove(firstDateTime);
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
