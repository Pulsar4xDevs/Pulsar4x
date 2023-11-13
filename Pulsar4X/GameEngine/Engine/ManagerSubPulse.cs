using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        [JsonProperty]
        public SortedDictionary<DateTime, Dictionary<string, List<Entity>>> InstanceProcessorsQueue { get; set; } = new ();

        [JsonProperty]
        public SafeDictionary<(Type processorType, Type dbType), DateTime?> HotLoopProcessorsNextRun { get; private set;} = new ();

        //public readonly ConcurrentDictionary<Type, TimeSpan> ProcessTime = new ConcurrentDictionary<Type, TimeSpan>();
        public bool IsProcessing = false;
        public string CurrentProcess = "Waiting";

        private ProcessorManager _processManager;

        private EntityManager _entityManager;

        internal Dictionary<DateTime, List<String>> GetInstanceProcForEntity(Entity entity)
        {
            var procDict = new Dictionary<DateTime, List<string>>();
            foreach (var (key, queue) in InstanceProcessorsQueue)
            {
                foreach(var (name, list) in queue)
                {
                    if(list.Contains(entity))
                    {
                        if(!procDict.ContainsKey(key))
                            procDict.Add(key, new List<string>());
                    }
                    procDict[key].Add(name);
                }
            }

            return procDict;
        }

        internal void ImportProcDictForEntity(Entity entity, Dictionary<DateTime, List<string>> procDict)
        {
            foreach (var kvp in procDict)
            {
                if(kvp.Key < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");

                if (!InstanceProcessorsQueue.ContainsKey(kvp.Key))
                    InstanceProcessorsQueue.Add(kvp.Key, new Dictionary<string, List<Entity>>());
                foreach (var procName in kvp.Value)
                {
                    if (!InstanceProcessorsQueue[kvp.Key].ContainsKey(procName))
                        InstanceProcessorsQueue[kvp.Key].Add(procName, new List<Entity>());
                    if (!InstanceProcessorsQueue[kvp.Key][procName].Contains(entity))
                        InstanceProcessorsQueue[kvp.Key][procName].Add(entity);
                }
            }
        }

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
        }

        internal void Initialize(EntityManager entityManager, ProcessorManager processorManager)
        {
            // Run the history once so it has some data to return to the UI
            Performance.BeginInterval();
            Performance.EndInterval();
            _game = entityManager.Game;
            _systemLocalDateTime = entityManager.Game.TimePulse.GameGlobalDateTime;
            _processToDateTime = _systemLocalDateTime;
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
                AddSystemInterupt(StarSysDateTime + item.Value.FirstRunOffset, item.Value);
            }
        }

        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, string actionProcessor, Entity? entity)
        {
            if(entity == null) throw new ArgumentNullException("Entity cannot be null");
            if(nextDateTime < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");
            if (nextDateTime < _processToDateTime)
                _processToDateTime = nextDateTime;
            if (!InstanceProcessorsQueue.ContainsKey(nextDateTime))
                InstanceProcessorsQueue.Add(nextDateTime, new Dictionary<string, List<Entity>>());
            if (!InstanceProcessorsQueue[nextDateTime].ContainsKey(actionProcessor))
                InstanceProcessorsQueue[nextDateTime].Add(actionProcessor, new List<Entity>());
            if (!InstanceProcessorsQueue[nextDateTime][actionProcessor].Contains(entity))
                InstanceProcessorsQueue[nextDateTime][actionProcessor].Add(entity);
        }


        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, IHotloopProcessor actionProcessor)
        {
            if(nextDateTime < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");

            Type processorType = actionProcessor.GetType();
            Type dbType = actionProcessor.GetParameterType;

            if(!HotLoopProcessorsNextRun.ContainsKey((processorType, dbType)))
            {
                HotLoopProcessorsNextRun.Add((processorType, dbType), nextDateTime);
            }
            else
            {
                // We only want to set the next run time if it is currently null
                // if it isn't null then it will already be queued to run!
                if(HotLoopProcessorsNextRun[(processorType, dbType)] == null)
                    HotLoopProcessorsNextRun[(processorType, dbType)] = nextDateTime;
            }
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

            AddSystemInterupt(nextDT, proc);
        }

    /// <summary>
        /// removes all references of an entity from the dictionary
        /// </summary>
        /// <param name="entity"></param>
        internal void RemoveEntity(Entity entity)
        {
            //possibly need to implement a reverse dictionary so entities can be looked up backwards, rather than itterating through?
            //MUST remove empty entries in the dictionary as an empty entitylist will be seen as a systemInterupt.
            //throw new NotImplementedException();

            List<DateTime> removekeys = new List<DateTime>();
            foreach (var (dateTime, dict) in InstanceProcessorsQueue)
            {
                foreach(var (key, list) in dict)
                {
                    list.Remove(entity);
                }

                if(dict.Values.Count == 0)
                    removekeys.Add(dateTime);
            }

            foreach (var item in removekeys)
            {
                InstanceProcessorsQueue.Remove(item);
            }

        }

        /// <summary>
        /// transfers all references from this starSystem to the new one
        /// Note that doing this could cause a temporal anomaly if the system we're moving to is ahead of this one.
        /// This should only be done from the MasterTimePulse when it has synched the systems.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="starsys"></param>
        internal void TransferEntity(Entity entity, StarSystem starsys)
        {

            Dictionary<DateTime, List<string>> procDict = GetInstanceProcForEntity(entity);

            RemoveEntity(entity);

            //add the processors to the new system
            starsys.ManagerSubpulses.ImportProcDictForEntity(entity, procDict);
        }


        internal void ProcessSystem(DateTime targetDateTime)
        {
            if(targetDateTime < StarSysDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow.
            //the system may need to run several times for a target datetime
            //keep processing the system till we've reached the wanted datetime
            Performance.BeginInterval();
            IsProcessing = true;
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

            if (InstanceProcessorsQueue.Keys.Count != 0 && nextInteruptDateTime >= InstanceProcessorsQueue.Keys.Min())
            {
                nextInteruptDateTime = InstanceProcessorsQueue.Keys.Min();
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
            TimeSpan span = (_processToDateTime - _systemLocalDateTime);
            int deltaSeconds = (int)span.TotalSeconds;

            foreach(var (type, runAt) in HotLoopProcessorsNextRun)
            {
                if(runAt == null || runAt > _processToDateTime)
                    continue;

                Performance.Start(type.processorType.Name);
                CurrentProcess = type.ToString();
                int count = _processManager.HotloopProcessors[type.dbType].ProcessManager(_entityManager, deltaSeconds);
                Performance.Stop(type.processorType.Name);

                if(count == 0)
                    HotLoopProcessorsNextRun[type] = null;
                else
                    HotLoopProcessorsNextRun[type] = _processToDateTime + _processManager.HotloopProcessors[type.dbType].RunFrequency; //sets the next interupt for this hotloop process
            }

            if (InstanceProcessorsQueue.ContainsKey(_processToDateTime))
            {
                var qp = InstanceProcessorsQueue[_processToDateTime];

                foreach(var instanceProcessSet in qp)
                {
                    var processor = _processManager.GetInstanceProcessor(instanceProcessSet.Key);
                    Performance.Start(processor.GetType().Name);
                    CurrentProcess = instanceProcessSet.Key;
                    foreach (var entity in instanceProcessSet.Value)
                    {

                        processor.ProcessEntity(entity, _processToDateTime);
                    }
                    Performance.Stop(processor.GetType().Name);
                }
                InstanceProcessorsQueue.Remove(_processToDateTime); //once all the processes have been run for that datetime, remove it from the dictionary.
            }
            StarSysDateTime = _processToDateTime; //update the localDateTime and invoke the SystemDateChangedEvent
        }

        public int GetTotalNumberOfProceses()
        {
            int i = 0;
            foreach (var processSet in InstanceProcessorsQueue)
            {
                i += processSet.Value.Count;
            }

            return i;
        }

        public List<DateTime> GetInteruptDateTimes()
        {
            List<DateTime> dates = new List<DateTime>();
            foreach (var item in InstanceProcessorsQueue)
            {
                dates.Add(item.Key);
            }
            return dates;
        }

    }
}


