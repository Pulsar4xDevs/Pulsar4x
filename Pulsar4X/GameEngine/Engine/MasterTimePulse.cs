using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using Pulsar4X.Events;
using System.Threading;

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

        /// <summary>
        /// A timer that generates a tick for the continious time simulation.
        /// 
        /// It is used to introduce a realtime delay between individual ticks.
        /// </summary>
        /// <remarks>
        /// If set to <see langword="null"/> the time simulation will proceed as fast as possible, with no delay between ticks.
        /// </remarks>
        private PeriodicTimer? _tickSource = null;

        /// <summary>
        /// Stores the task that is running the active time simulation.
        /// 
        /// If <see langword="null"/>, no time simulation is active. 
        /// </summary>
        private Task? _timeSimulationTask = null;
        private CancellationTokenSource? _timeSimulationCts = null;

        /// <summary>
        /// Returns true if the time loop is currently running (not paused)
        /// </summary>
        [JsonIgnore]
        public bool IsRunning => _timeSimulationTask is not null && !_timeSimulationTask.IsCompleted;

        /// <summary>
        /// Returns <see langword="true"/> if the time loop is running and has a pending stop request.
        /// </summary>
        [JsonIgnore]
        public bool IsStopping => IsRunning && (_timeSimulationCts?.IsCancellationRequested ?? false);

        [JsonIgnore]
        private TimeSpan _tickInterval = TimeSpan.FromMilliseconds(100);

        [JsonProperty]
        public TimeSpan TickFrequency
        {
            get { return _tickInterval; }
            set
            {
                // Prevent values outside PeriodicTimer's supported range.
                TimeSpan minTickInterval = TimeSpan.FromMilliseconds(1);
                TimeSpan maxTickInterval = TimeSpan.FromMilliseconds(uint.MaxValue - 1d);

                if (value < minTickInterval)
                    _tickInterval = minTickInterval;
                else if (value > maxTickInterval)
                    _tickInterval = maxTickInterval;
                else
                    _tickInterval = value;

                if (_tickSource is not null)
                    _tickSource.Period = _tickInterval;
            }
        }

        [JsonProperty]
        public TimeSpan Ticklength { get; set; } = TimeSpan.FromSeconds(3600);

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
            Event e = Event.Create(EventType.GlobalDateChange, GameGlobalDateTime, "Game Global Date Change");
            EventManager.Instance.Publish(e);

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
            _tickSource = new PeriodicTimer(_tickInterval);
        }

        #region Public Time Methods. UI interacts with time here

        /// <summary>
        /// Pauses the timeloop
        /// </summary>
        public void PauseTime()
        {
            // Requests a simulation halt if it is running.
            _timeSimulationCts?.Cancel();
        }
        /// <summary>
        /// Starts the timeloop
        /// </summary>
        public void StartTime()
        {
            // Check if we already have an active time simulation task
            if (IsRunning)
                return;

            // Start the continious time simulation task.
            _timeSimulationCts?.Dispose();
            _timeSimulationCts = new CancellationTokenSource();
            _timeSimulationTask = Task.Run(() => SimulateTimeAsync(_timeSimulationCts.Token), _timeSimulationCts.Token);
        }


        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep()
        {
            TimeStep(GameGlobalDateTime + Ticklength);
        }

        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep(DateTime toDate)
        {
            if (IsRunning)
                return;

            _timeSimulationCts?.Dispose();
            _timeSimulationCts = new CancellationTokenSource();
            _timeSimulationTask = Task.Run(() => SimulateTimeUntil(toDate, _timeSimulationCts.Token), _timeSimulationCts.Token);

            if (_game.Settings.EnforceSingleThread)
                _timeSimulationTask.Wait();
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

        /// <summary>
        /// Performs a continious time simulation, where ticks are generated by the _tickSource timer.
        /// 
        /// </summary>
        /// <param name="ct">Cancellation token to stop the simulation.</param>
        /// <returns></returns>
        /// <remarks>
        /// It is recommended to always call this method via <see cref="Task.Run"/> call, due to it being CPU bound.
        /// </remarks>
        private async Task SimulateTimeAsync(CancellationToken ct = default)
        {
            if (_tickSource is null)
            {
                if (!ct.CanBeCanceled)
                {
                    throw new InvalidOperationException("Simulation without a tick source requires a cancellation token.");
                }

                // Run the simulation as fast as possible, with no delay between ticks.
                while (!ct.IsCancellationRequested)
                {
                    SimulateTimeUntil(GameGlobalDateTime + Ticklength, ct);
                }
            }
            else
            {
                // If a tick source is set, use it to generate ticks.
                // The call to WaitForNextTickAsync will return `true` if the timer fired, or 'false' if the timer was disposed.
                while (await _tickSource.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    SimulateTimeUntil(GameGlobalDateTime + Ticklength, ct);
                }
            }
        }

        /// <summary>
        /// Runs the simulation until the specified target date time is reached.
        /// </summary>
        /// <param name="targetDateTime"></param>
        /// <param name="ct">Cancellation token to signal asynchronous stop request.</param>
        private void SimulateTimeUntil(DateTime targetDateTime, CancellationToken ct = default)
        {
            _stopwatch.Start(); //start the processor loop stopwatch (performance counter)

            // If a cancellation is signalled, stop the time advance the next time an interrupt happens.
            while (GameGlobalDateTime < targetDateTime && !ct.IsCancellationRequested)
            {
                _subpulseStopwatch.Start();
                DateTime nextInterupt = ProcessNextInterupt(targetDateTime);
                //do system processors
                var activeSystems = _game.Systems.Where(s => s.ActivityState != SystemActivityState.Stasis);

                if (_game.Settings.EnableMultiThreading == true)
                {
                    //multi-threaded
                    Parallel.ForEach(activeSystems, starSys => starSys.ManagerSubpulses.ProcessSystem(nextInterupt));

                    //The above 'blocks' till all the tasks are done.
                }
                else
                {
                    // single-threaded
                    foreach (StarSystem starSys in activeSystems)
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
        }

        private DateTime ProcessNextInterupt(DateTime maxDateTime)
        {
            if (EntityDictionary.Keys.Count == 0) return maxDateTime;

            DateTime nextInteruptDateTime = EntityDictionary.Keys.Min();

            if (nextInteruptDateTime > maxDateTime) return maxDateTime;

            foreach (var delegateListPair in EntityDictionary[nextInteruptDateTime])
            {
                foreach (var jumpPair in delegateListPair.Value) //foreach entity in the value list
                {
                    //delegateListPair.Key.DynamicInvoke(_game, jumpPair);
                    PulseActionDictionary.DoAction(delegateListPair.Key, _game, jumpPair);
                }

            }
            return nextInteruptDateTime;
        }

        public bool Equals(MasterTimePulse? other)
        {
            if (other is null) return false;

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
