using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Pulsar4X.DataStructures;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.Engine
{
    public delegate void DateChangedEventHandler(DateTime newDate);

    //[JsonConverter(typeof(MasterTimePulseConverter))]
    public class MasterTimePulse : IEquatable<MasterTimePulse>
    {
        [JsonProperty]
        internal SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>>();

        [JsonIgnore]
        private Stopwatch _stopwatch = new Stopwatch();

        [JsonIgnore]
        Stopwatch _subpulseStopwatch = new Stopwatch();

        [JsonIgnore]
        private Timer _timer = new Timer();

        [JsonIgnore]
        private Action<MasterTimePulse> runSystemProcesses = (MasterTimePulse obj) =>
        {
            obj.DoProcessing(obj.GameGlobalDateTime + obj.Ticklength);
        };

        [JsonProperty]
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

        [JsonIgnore]
        private float _timeMultiplier = 1f;

        [JsonIgnore]
        private TimeSpan _tickInterval = TimeSpan.FromMilliseconds(250);

        [JsonProperty]
        public TimeSpan TickFrequency
        {
            get { return _tickInterval; }
            set
            {
                _tickInterval = value;
                _timer.Interval = _tickInterval.TotalMilliseconds * _timeMultiplier;
            }
        }

        [JsonProperty]
        public TimeSpan Ticklength { get; set; } = TimeSpan.FromSeconds(3600);

        [JsonIgnore]
        private bool _isProcessing = false;

        [JsonIgnore]
        private bool _isOvertime = false;

        [JsonIgnore]
        private object _lockObj = new object();

        [JsonIgnore]
        private Game _game;

        /// <summary>
        /// length of time it took to process the last DoProcess
        /// </summary>
        [JsonProperty]
        public TimeSpan LastProcessingTime { get; internal set; } = TimeSpan.Zero;

        [JsonProperty]
        public TimeSpan LastSubtickTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>
        /// This invokes the DateChangedEvent.
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {
            // Event logevent = new Event(GameGlobalDateTime, "Game Global Date Changed", null, null, null);
            // logevent.EventType = EventType.GlobalDateChange;
            // StaticRefLib.EventLog.AddEvent(logevent);

            GameGlobalDateChangedEvent?.Invoke(GameGlobalDateTime);
        }

        [JsonIgnore]
        private DateTime _gameGlobalDateTime;

        [JsonProperty]
        public DateTime GameGlobalDateTime
        {
            get { return _gameGlobalDateTime; }
            internal set
            {
                _gameGlobalDateTime = value;
                // FIXME: needs to get rid of StaticRefLib references
                // if (StaticRefLib.SyncContext != null)
                //     StaticRefLib.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread, so the event is invoked on that thread.
                // else
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
        internal MasterTimePulse(Game game)
        {
            _game = game;
            _gameGlobalDateTime = game.Settings.StartDateTime;
        }

        public MasterTimePulse() { }

        public void Initialize(Game game)
        {
            _game = game;
            _timer.Interval = _tickInterval.TotalMilliseconds;
            _timer.Enabled = false;
            _timer.Elapsed += Timer_Elapsed;
        }

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
            if (_isProcessing)
                return;

            Task tsk = Task.Run(() => DoProcessing(GameGlobalDateTime + Ticklength));

            if (_game.Settings.EnforceSingleThread)
                tsk.Wait();

            _timer.Stop();
        }

        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep(DateTime toDate)
        {
            if (_isProcessing)
                return;

            Task tsk = Task.Run(() => DoProcessing(toDate));

            if (_game.Settings.EnforceSingleThread)
                tsk.Wait();

            _timer.Stop();
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
            if (!EntityDictionary.ContainsKey(datetime))
                EntityDictionary.Add(datetime, new Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>());
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
                DoProcessing(GameGlobalDateTime + Ticklength); //run DoProcessing if we're not already processing
            }
            else
            {
                lock (_lockObj)
                {
                   _isOvertime = true; //if we're processing, then processing it taking longer than the sim speed
                }
            }
        }



        private void DoProcessing(DateTime targetDateTime)
        {
            lock (_lockObj)
            {//would it be better to just put this whole function within this lock?
                _isProcessing = true;
                _isOvertime = false;
            }

            if(_timer.Enabled)
            {
                _timer.Stop();
                _timer.Start(); //reset timer so we're counting from 0
            }
            _stopwatch.Start(); //start the processor loop stopwatch (performance counter)

            //check for global interupts
            //_targetDateTime = GameGlobalDateTime + Ticklength;


            while (GameGlobalDateTime < targetDateTime)
            {
                _subpulseStopwatch.Start();
                DateTime nextInterupt = ProcessNextInterupt(targetDateTime);
                //do system processors

                if (_game.Settings.EnableMultiThreading == true)
                {
                    //multi-threaded
                    Parallel.ForEach<StarSystem>(_game.Systems, starSys => starSys.ManagerSubpulses.ProcessSystem(nextInterupt));

                    //The above 'blocks' till all the tasks are done.
                }
                else
                {
                    // single-threaded
                    foreach (StarSystem starSys in _game.Systems)
                    {
                        starSys.ManagerSubpulses.ProcessSystem(nextInterupt);
                    }
                }

                LastSubtickTime = _subpulseStopwatch.Elapsed;
                GameGlobalDateTime = nextInterupt; //set the GlobalDateTime this will invoke the datechange event.
                _subpulseStopwatch.Reset();
            }

            LastProcessingTime = _stopwatch.Elapsed; //how long the processing took
            _stopwatch.Reset();

            lock (_lockObj)
            {
                _isProcessing = false;
            }
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
                            PulseActionDictionary.DoAction(delegateListPair.Key, _game, jumpPair);
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



        public bool Equals(MasterTimePulse other)
        {
            bool equality = false;
            if (GameGlobalDateTime.Equals(other.GameGlobalDateTime))
            {
                if (EntityDictionary.Count.Equals(other.EntityDictionary.Count))
                    equality = true;
            }
            return equality;
        }
    }

    // public class MasterTimePulseConverter : JsonConverter
    // {
    //     public override bool CanConvert(Type objectType) => objectType == typeof(MasterTimePulse);

    //     public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //     {
    //         // Save JObject to set it later in the second step
    //         JToken jsonObject = JToken.Load(reader);
    //         var gameProperty = serializer.Context.Context as Game;

    //         // If the Game property is already set, deserialize properties and initialize
    //         if (gameProperty != null)
    //         {
    //             var timePulse = new MasterTimePulse(gameProperty) {
    //                 GameGlobalDateTime = jsonObject["GameGlobalDateTime"].ToObject<DateTime>(serializer),
    //                 TimeMultiplier = jsonObject["TimeMultiplier"].ToObject<float>(serializer),
    //                 TickFrequency = jsonObject["TickFrequency"].ToObject<TimeSpan>(serializer),
    //                 Ticklength = jsonObject["Ticklength"].ToObject<TimeSpan>(serializer),
    //                 LastProcessingTime = jsonObject["LastProcessingTime"].ToObject<TimeSpan>(serializer),
    //                 LastSubtickTime = jsonObject["LastSubtickTime"].ToObject<TimeSpan>(serializer),
    //                 EntityDictionary = jsonObject["EntityDictionary"].ToObject<SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>>>(serializer)
    //             };
    //             return timePulse;
    //         }

    //         // If Game is not set, return null for now
    //         return null;
    //     }

    //     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //     {
    //         var pulse = (MasterTimePulse)value;

    //         var entityDictFieldInfo = typeof(MasterTimePulse).GetField("EntityDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //         var entityDictValue = entityDictFieldInfo?.GetValue(pulse);

    //         JObject obj = new JObject
    //         {
    //             { "GameGlobalDateTime", new JValue(pulse.GameGlobalDateTime) },
    //             { "TimeMultiplier", new JValue(pulse.TimeMultiplier) },
    //             { "TickFrequency", new JValue(pulse.TickFrequency) },
    //             { "Ticklength", new JValue(pulse.Ticklength) },
    //             { "LastProcessingTime", new JValue(pulse.LastProcessingTime) },
    //             { "LastSubtickTime", new JValue(pulse.LastSubtickTime) },
    //             { "EntityDictionary", new JObject(entityDictValue) }
    //         };
    //         obj.WriteTo(writer);
    //     }
    // }

}
