using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{

    /// <summary>
    /// handles and processes entities for a specific datetime.
    /// TODO:  handle removal of entities from the system.
    /// TODO:  handle removal of ability datablobs from an entity
    /// TODO:  handle passing an entity from this system to another, and carry it's subpulses/interupts across.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ManagerSubPulse
    {
        private Game _game;

        public PerformanceStopwatch Performance { get; private set; } = new PerformanceStopwatch();

        private readonly object _lock = new();
        [JsonProperty]
        private TimeQueue<(string, Entity)> _instanceProcessorsQueue = new();
        public TimeQueue<(string, Entity)> InstanceProcessorsQueue {
            get {
                lock (_lock)
                {
                    return new TimeQueue<(string, Entity)>(_instanceProcessorsQueue);
                }
            }
            private set {}
        }

        [JsonProperty]
        public SafeDictionary<Type , DateTime?> HotLoopProcessorsNextRun { get; private set;} = new();

        /// <summary>
        /// Multiplier applied to hotloop processor RunFrequency.
        /// 1.0 = normal (Foreground), >1.0 = slower (Background).
        /// </summary>
        [JsonProperty]
        public double FrequencyMultiplier { get; set; } = 1.0;

        //public readonly ConcurrentDictionary<Type, TimeSpan> ProcessTime = new ConcurrentDictionary<Type, TimeSpan>();
        public bool IsProcessing = false;
        public string CurrentProcess = "Waiting";

        private ProcessorManager _processManager;

        private EntityManager _entityManager;

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
            //Event logevent = new Event(_systemLocalDateTime, "System Date Change", _entityManager.ID, null, null, null);
            //logevent.EventType = EventType.SystemDateChange;
            //_entityManager.Game.EventLog.AddEvent(logevent);
            int threadID = Thread.CurrentThread.ManagedThreadId;
            SystemDateChangedEvent?.Invoke(StarSysDateTime);
        }


        [JsonProperty] private DateTime _systemLocalDateTime;
        private DateTime _processToDateTime;
        private DateTime _subStepDateTime;
        public DateTime StarSysDateTime
        {
            get { return _systemLocalDateTime; }
            private set
            {
                if (value < _systemLocalDateTime)
                    throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow.
                _systemLocalDateTime = value;
                // FIXME: needs to get rid of StaticRefLib references
                // if (StaticRefLib.SyncContext != null)
                //     StaticRefLib.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread, so the event is invoked on that thread.
                //NOTE: the above marshaling does not apear to work correctly, it's possible for it to work, the context needs to be in an await state or something.
                //do not rely on the above being run on the main thread! (maybe we should remove the marshaling?)
                // else //if context is null, we're probibly running tests or headless.
                //     InvokeDateChange(value); //in this case we're not going to marshal this. (event will fire on *THIS* thread)
            }
        }

        /// <summary>
        /// constructor for json
        /// </summary>
        [JsonConstructor]
        internal ManagerSubPulse()
        {
            InstanceProcessorsQueue = _instanceProcessorsQueue;
        }

        internal void Initialize(EntityManager entityManager, ProcessorManager processorManager)
        {
            // Run the history once so it has some data to return to the UI
            Performance.BeginInterval();
            Performance.EndInterval();
            _game = entityManager.Game;
            _systemLocalDateTime = entityManager.Game.TimePulse.GameGlobalDateTime;
            _processToDateTime = _systemLocalDateTime;
            _subStepDateTime = _systemLocalDateTime;
            _entityManager = entityManager;
            _processManager = processorManager;
            InitHotloopProcessors();
        }

        internal void PostLoadInit(EntityManager entityManager) //this one is used after loading a game.
        {
            _entityManager = entityManager;
            _processManager = entityManager.Game.ProcessorManager;
        }

        private void InitHotloopProcessors()
        {
            foreach (var item in _processManager.HotloopProcessors)
            {
                //the date time here is going to be inconsistant when a game is saved then loaded, vs running without a save/load. needs fixing.
                //also we may want to run many of these before the first turn, and still have this offset.
                AddSystemInterupt(StarSysDateTime + item.Value.FirstRunOffset, item.Key);
            }
        }

        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, string actionProcessor, Entity entity)
        {
            if(nextDateTime < StarSysDateTime)
                throw new Exception("Trying to add an interrupt in the past");

            if (nextDateTime < _subStepDateTime)
                _subStepDateTime = nextDateTime;

            lock (_lock)
            {
                _instanceProcessorsQueue.Add(nextDateTime, (actionProcessor, entity));
            }
        }

        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, Type dbType)
        {
            if(!dbType.IsSubclassOf(typeof(BaseDataBlob)))
            {
                throw new Exception("Trying to add non datablob type");
            }
            if(!HotLoopProcessorsNextRun.ContainsKey((dbType)))
            {
                HotLoopProcessorsNextRun.Add((dbType), nextDateTime);
            }
            else
            {
                // We only want to set the next run time if it is currently null
                // if it isn't null then it will already be queued to run!
                if(HotLoopProcessorsNextRun[(dbType)] == null)
                    HotLoopProcessorsNextRun[(dbType)] = nextDateTime;
            }
        }

        /// <summary>
        /// Gets the run frequency for a hotloop processor by its datablob type.
        /// </summary>
        /// <param name="dbType">The datablob type associated with the processor</param>
        /// <returns>The run frequency TimeSpan, or null if the processor is not found</returns>
        public TimeSpan? GetProcessorRunFrequency(Type dbType)
        {
            if (_processManager != null && _processManager.HotloopProcessors.TryGetValue(dbType, out var processor))
                return processor.RunFrequency;
            return null;
        }


        internal void AddSystemInterupt(BaseDataBlob db)
        {
            //we need to use _processToDateTime in this function instead of StarSysDateTime (or _systemLocalDateTime)
            //due to this method being called while/by a child of the "ProcessToNextInterupt()" function is running.
            //ie if a datablob gets added to the manager, this gets called. a datablob can get added at any time.
            //we want to add processors to the correct timeslots (processor offset and frequency)
            //using StarSysDateTime we were adding a processor in a timeslot that would end up after the current datetime,
            //but before the NextInterupt dateTime, which would cause a Temporal Anomaly Exception.

            if (!_game.ProcessorManager.HotloopProcessors.ContainsKey(db.GetType()))
                return;
            var proc = _game.ProcessorManager.HotloopProcessors[db.GetType()];

            DateTime startDate = _game.Settings.StartDateTime;
            var elapsed = _processToDateTime - startDate;
            elapsed -= proc.FirstRunOffset;

            var nextInSec = proc.RunFrequency.TotalSeconds - elapsed.TotalSeconds % proc.RunFrequency.TotalSeconds;
            var next = TimeSpan.FromSeconds(nextInSec);
            DateTime nextDT = _processToDateTime + next;

            if(nextDT < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");

            Type dbType = db.GetType();
            AddSystemInterupt(nextDT, dbType);

        }

        /// <summary>
        /// removes all references of an entity from the dictionary
        /// </summary>
        /// <param name="entity"></param>
        internal void RemoveEntity(Entity entity)
        {
            //possibly need to implement a reverse dictionary so entities can be looked up backwards, rather than itterating through?

            List<int> removekeys = new();
            var idx = 0;

            lock (_lock)
            {
                foreach (var qi in _instanceProcessorsQueue)
                {
                    if (qi.Item.Item2 == entity)
                        removekeys.Add(idx);
                    idx += 1;
                }

                foreach (var i in removekeys)
                    _instanceProcessorsQueue.RemoveAt(i);
            }
        }

        internal void FastForwardTo(DateTime targetDateTime)
        {
            _systemLocalDateTime = targetDateTime;
            _processToDateTime = targetDateTime;
            _subStepDateTime = targetDateTime;

            var types = HotLoopProcessorsNextRun.Keys.ToList();
            foreach (var type in types)
            {
                if (HotLoopProcessorsNextRun[type] == null)
                    continue;
                var proc = _processManager.HotloopProcessors[type];
                HotLoopProcessorsNextRun[type] = targetDateTime + proc.FirstRunOffset;
            }

            lock (_lock)
            {
                _instanceProcessorsQueue = new TimeQueue<(string, Entity)>();
            }
        }

        internal void ProcessSystem(DateTime targetDateTime)
        {
            if(targetDateTime < StarSysDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow.
            //the system may need to run several times for a target datetime
            //keep processing the system till we've reached the wanted datetime
            Performance.BeginInterval();
            IsProcessing = true;

            // TODO: fix this. it doesn't let time progress in SM mode
            // if (!SpinWait.SpinUntil(_entityManager.HaveAllListnersProcessed, TimeSpan.FromMilliseconds(500)))
            //     throw new Exception("timeout on listnerProcessing.");

            _entityManager.RemoveTaggedEntitys();
            while (StarSysDateTime < targetDateTime)
            {
                Performance.BeingSubInterval();
                //calculate max time the system can run/time to next interupt
                //this should handle predicted events, ie econ, production, shipjumps, sensors etc.
                TimeSpan timeDeltaMax = targetDateTime - StarSysDateTime;

                //this bit is a bit messy, we're storing this as a class variable
                //the reason we're storing it, is because one of the functions (AddSystemInterupt)
                    //is called from elsewhere, possibly during the processing loop.
                    //we may need to make this more flexable and shorten the processing loop if this happens?
                    //that might cause issues elsewhere.
                _processToDateTime = GetNextInterupt(timeDeltaMax);
                _subStepDateTime = _processToDateTime;
                ProcessToNextInterupt();

                Performance.EndSubInterval();
            }

            CurrentProcess = "Waiting";
            Performance.EndInterval();

            IsProcessing = false;
        }

        private DateTime GetNextInterupt(TimeSpan maxSpan)
        {
            DateTime nextInteruptDateTime = StarSysDateTime + maxSpan;

            if(HotLoopProcessorsNextRun.Count > 0 && nextInteruptDateTime >= HotLoopProcessorsNextRun.Values.Min())
            {
                nextInteruptDateTime = HotLoopProcessorsNextRun.Values.Min() ?? nextInteruptDateTime;
            }

            lock (_lock)
            {
                if (_instanceProcessorsQueue.Any() && nextInteruptDateTime >= _instanceProcessorsQueue.First().Time)
                    nextInteruptDateTime = _instanceProcessorsQueue.First().Time;
            }

            if (nextInteruptDateTime < StarSysDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow.
            return nextInteruptDateTime;
        }

        /// <summary>
        /// process to next subpulse
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="maxSpan">maximum time delta</param>
        /// <returns>datetime processed to</returns>
        private void ProcessToNextInterupt()
        {
            while (StarSysDateTime <= _processToDateTime)
            {
                TimeSpan span = (_subStepDateTime - _systemLocalDateTime);
                int deltaSeconds = (int)span.TotalSeconds;

                foreach (var (type, runAt) in HotLoopProcessorsNextRun)
                {
                    if (runAt == null || runAt > _subStepDateTime)
                        continue;

                    Trace.WriteLine(String.Format("[{0:u}|{1:u}] running hotloop processor: {2} with entity manager: {3}",
                                StarSysDateTime, _subStepDateTime, type.Name, _entityManager.ManagerID));

                    Performance.Start(_entityManager.ManagerID + "-" + type.Name);
                    CurrentProcess = type.ToString();
                    var proc = _game.ProcessorManager.HotloopProcessors[type];
                    int count = proc.ProcessManager(_entityManager, deltaSeconds);
                    Performance.Stop(_entityManager.ManagerID + "-" + type.Name);

                    if (count == 0)
                        HotLoopProcessorsNextRun[type] = null;
                    else
                    {
                        var baseFrequency = _processManager.HotloopProcessors[type].RunFrequency;
                        var scaledFrequency = TimeSpan.FromTicks((long)(baseFrequency.Ticks * FrequencyMultiplier));
                        HotLoopProcessorsNextRun[type] = _subStepDateTime + scaledFrequency;
                    }
                }

                TimeQueueItem<(string, Entity)>[] split;
                lock (_lock)
                {
                    split = _instanceProcessorsQueue.Split(_subStepDateTime);
                }

                foreach (var qi in split)
                {
                    var itm = qi.Item;
                    var s = itm.Item1;
                    var e = itm.Item2;

                    var processor = _processManager.GetInstanceProcessor(s);
                    var pn = processor.GetType().Name;

                    Trace.WriteLine(String.Format("[{0:u}|{1:u}] running instance processor: {2} with entity: {3}",
                                StarSysDateTime, _subStepDateTime, pn, e.DebuggerDisplay));

                    Performance.Start(pn);
                    CurrentProcess = s;
                    processor.ProcessEntity(e, qi.Time);
                    Performance.Stop(pn);
                }

                StarSysDateTime = _subStepDateTime; //update the localDateTime and invoke the SystemDateChangedEvent
                _subStepDateTime = GetNextInterupt(_processToDateTime - _subStepDateTime);

                // Need to run this on each sub-step otherwise processors will continue to be called on subsequent sub-steps when they shouldn't
                _entityManager.RemoveTaggedEntitys();

                //this lets us run through at least once.
                if(StarSysDateTime == _processToDateTime)
                    break;
            }
        }
    }
}
