using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

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

        public struct PerformanceData
        {
            public double FullPulseTimeMS;
            public double[] SubpulseTimes;
            public (string pname, double[] ptimes, double psum)[] ProcessTimes;
        }

        Stopwatch _masterPulseStopwatch = new Stopwatch();
        Stopwatch _subPulseStopwatch = new Stopwatch();
        Stopwatch _processStopwatch = new Stopwatch();

        private double _fullPulseTimeMS = double.PositiveInfinity;
        private List<double> _subpulseTimes = new List<double>();
        private Dictionary<string, List<double>> _detailedProcessTimes = new Dictionary<string, List<double>>();

        public PerformanceData[] PerfHistory = new PerformanceData[1];
        public int PerfHistoryIndex { get; private set; }

        private Game _game;

        /// <summary>
        /// Threadsafe Change the amount of history we store for this Manager.
        /// </summary>
        public int PerfHistoryCount
        {
            get { return PerfHistory.Length; }
            set
            {
                lock (PerfHistory)
                {
                    int copyCount = Math.Min(PerfHistory.Length, value);
                    var oldHistory = PerfHistory;
                    PerfHistory = new PerformanceData[value];
                    Buffer.BlockCopy(oldHistory, 0, PerfHistory, 0, copyCount);
                    if (PerfHistoryIndex > value - 1)
                        PerfHistoryIndex = value - 1;
                }
            }
        }

        [JsonProperty] public SortedDictionary<DateTime, ProcessSet> QueuedProcesses { get; set; } = new SortedDictionary<DateTime, ProcessSet>();

        //public readonly ConcurrentDictionary<Type, TimeSpan> ProcessTime = new ConcurrentDictionary<Type, TimeSpan>();
        public bool IsProcessing = false;
        public string CurrentProcess = "Waiting";

        private ProcessorManager _processManager;

        private EntityManager _entityManager;

        public class ProcessSet : ISerializable
        {

            [JsonProperty] //this should get added on initialization.
            internal List<IHotloopProcessor> SystemProcessors = new List<IHotloopProcessor>();

            [JsonProperty] //this needs to get saved. need to check that entities here are saved as guids in the save file and that they get re-referenced on load too (should happen if the serialization manager does its job properly).
            internal Dictionary<string, List<Entity>> InstanceProcessors = new Dictionary<string, List<Entity>>();

            //todo: need to get a list of InstanceProcessors that have entites owned by a specific faction.

            internal ProcessSet()
            {
            }

            // FIXME: needs to get rid of StaticRefLib references
            // internal ProcessSet(SerializationInfo info, StreamingContext context)
            // {
            //     Game game = (Game)context.Context;
            //     Dictionary<string, List<Guid>> instanceProcessors = (Dictionary<string, List<Guid>>)info.GetValue(nameof(InstanceProcessors), typeof(Dictionary<string, List<Guid>>));
            //     ProcessorManager processManager = StaticRefLib.ProcessorManager;
            //     foreach (var kvpItem in instanceProcessors)
            //     {

            //         string typeName = kvpItem.Key;

            //         //IInstanceProcessor processor = processManager.GetInstanceProcessor(typeName);
            //         if (!InstanceProcessors.ContainsKey(typeName))
            //             InstanceProcessors.Add(typeName, new List<Entity>());

            //         foreach (var entityGuid in kvpItem.Value)
            //         {
            //             if (game.GlobalManager.FindEntityByGuid(entityGuid, out Entity entity)) //might be a better way to do this, can we get the manager from here and just search localy?
            //             {
            //                 InstanceProcessors[typeName].Add(entity);
            //             }
            //             else
            //             {
            //                 // Entity has not been deserialized.
            //                 // throw new Exception("Unfound Entity Exception, possibly this entity hasn't been deseralised yet?"); //I *think* we'll have the entitys all deseralised for this manager at this point...
            //             }
            //         }
            //     }
            // }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                var instanceProcessors = new Dictionary<string, List<int>>();
                foreach (var kvpitem in InstanceProcessors)
                {
                    string typeName = kvpitem.Key; //.TypeName;
                    instanceProcessors.Add(typeName, new List<int>());
                    foreach (var entityItem in kvpitem.Value)
                    {
                        instanceProcessors[typeName].Add(entityItem.Id);
                    }
                }

                info.AddValue(nameof(InstanceProcessors), instanceProcessors);
            }


            internal List<string> GetInstanceProcForEntity(Entity entity)
            {
                var procList = new List<String>();


                foreach (var kvp in InstanceProcessors)
                {
                    if (kvp.Value.Contains(entity))
                        procList.Add(kvp.Key);
                }

                return procList;
            }

            internal List<string> RemoveEntity(Entity entity)
            {
                var procList = new List<String>();
                var removelist = new List<string>();
                foreach (var kvp in InstanceProcessors)
                {
                    if (kvp.Value.Contains(entity))
                        kvp.Value.Remove(entity);
                    procList.Add(kvp.Key);
                    if (kvp.Value.Count == 0)
                        removelist.Add(kvp.Key);
                }

                foreach (var item in removelist)
                {
                    InstanceProcessors.Remove(item);
                }

                return procList;
            }

            internal bool IsEmpty()
            {
                if (InstanceProcessors.Count == 0 && SystemProcessors.Count == 0)
                    return true;
                return false;
            }
        }


        private void AddPerfHistory()
        {
            if (PerfHistoryCount <= 0)
                return;


            (string pname, double[] ptimes, double psum)[] pTimes = new (string pname, double[] ptimes, double psum)[_detailedProcessTimes.Count];

            int i1 = 0;
            foreach (var kvp in _detailedProcessTimes)
            {
                var array = kvp.Value.ToArray();

                double sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }

                pTimes[i1] = (kvp.Key, array, sum);
                i1++;
            }


            PerformanceData newData = new PerformanceData() { FullPulseTimeMS = _fullPulseTimeMS, SubpulseTimes = _subpulseTimes.ToArray(), ProcessTimes = pTimes };

            lock (PerfHistory)
            {
                PerfHistoryIndex++;
                if (PerfHistoryIndex >= PerfHistoryCount)
                    PerfHistoryIndex = 0;

                PerfHistory[PerfHistoryIndex] = newData;
            }
        }

        public PerformanceData GetLastPerfData()
        {
            lock (PerfHistory)
            {
                return PerfHistory[PerfHistoryIndex];
            }
        }

        internal Dictionary<DateTime, List<String>> GetInstanceProcForEntity(Entity entity)
        {
            var procDict = new Dictionary<DateTime, List<string>>();
            foreach (var kvp in QueuedProcesses)
            {
                var procList = kvp.Value.GetInstanceProcForEntity(entity);
                if (procList.Count > 0)
                    procDict.Add(kvp.Key, procList);
            }

            return procDict;
        }

        internal void ImportProcDictForEntity(Entity entity, Dictionary<DateTime, List<string>> procDict)
        {
            foreach (var kvp in procDict)
            {
                if(kvp.Key < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");

                if (!QueuedProcesses.ContainsKey(kvp.Key))
                    QueuedProcesses.Add(kvp.Key, new ProcessSet());
                foreach (var procName in kvp.Value)
                {
                    if (!QueuedProcesses[kvp.Key].InstanceProcessors.ContainsKey(procName))
                        QueuedProcesses[kvp.Key].InstanceProcessors.Add(procName, new List<Entity>());
                    if (!QueuedProcesses[kvp.Key].InstanceProcessors[procName].Contains(entity))
                        QueuedProcesses[kvp.Key].InstanceProcessors[procName].Add(entity);
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
            AddPerfHistory();
        }

        internal void Initialize(EntityManager entityManager, ProcessorManager processorManager)
        {
            (string pname, double[] ptimes, double psum)[] pTimes = new (string pname, double[] ptimes, double psum)[] { };
            PerformanceData newData = new PerformanceData() { FullPulseTimeMS = _fullPulseTimeMS, SubpulseTimes = _subpulseTimes.ToArray(), ProcessTimes = pTimes };
            PerfHistory[PerfHistoryIndex] = newData;

            _game = entityManager.Game;
            _systemLocalDateTime = entityManager.Game.TimePulse.GameGlobalDateTime;
            _processToDateTime = _systemLocalDateTime;
            _entityManager = entityManager;
            _processManager = processorManager;
            InitHotloopProcessors();
        }

        internal void PostLoadInit(StreamingContext context, EntityManager entityManager) //this one is used after loading a game.
        {
            _entityManager = entityManager;
            _processManager = entityManager.Game.ProcessorManager;
            InitHotloopProcessors();
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
        internal void AddEntityInterupt(DateTime nextDateTime, string actionProcessor, Entity entity)
        {
            if(nextDateTime < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");
            if (!QueuedProcesses.ContainsKey(nextDateTime))
                QueuedProcesses.Add(nextDateTime, new ProcessSet());
            if (!QueuedProcesses[nextDateTime].InstanceProcessors.ContainsKey(actionProcessor))
                QueuedProcesses[nextDateTime].InstanceProcessors.Add(actionProcessor, new List<Entity>());
            if (!QueuedProcesses[nextDateTime].InstanceProcessors[actionProcessor].Contains(entity))
                QueuedProcesses[nextDateTime].InstanceProcessors[actionProcessor].Add(entity);
        }


        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, IHotloopProcessor actionProcessor)
        {
            if(nextDateTime < StarSysDateTime) throw new Exception("Trying to add an interrupt in the past");

            if (!QueuedProcesses.ContainsKey(nextDateTime))
                QueuedProcesses.Add(nextDateTime, new ProcessSet());
            if (!QueuedProcesses[nextDateTime].SystemProcessors.Contains(actionProcessor))
                QueuedProcesses[nextDateTime].SystemProcessors.Add(actionProcessor);
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

            if(!QueuedProcesses.ContainsKey(nextDT))
                QueuedProcesses.Add(nextDT, new ProcessSet());
            if (!QueuedProcesses[nextDT].SystemProcessors.Contains(proc))
                QueuedProcesses[nextDT].SystemProcessors.Add(proc);
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
            foreach (var kvp in QueuedProcesses)
            {
                kvp.Value.RemoveEntity(entity);
                if(kvp.Value.IsEmpty())
                    removekeys.Add(kvp.Key);
            }

            foreach (var item in removekeys)
            {
                QueuedProcesses.Remove(item);
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
            List<DateTime> removekeys = new List<DateTime>();

            //get the dates and processors associated with this entity
            foreach (var kvp in QueuedProcesses)
            {
                var procs = kvp.Value.RemoveEntity(entity);
                if(procs.Count > 0)
                    procDict.Add(kvp.Key, procs);
                if(kvp.Value.IsEmpty())
                    removekeys.Add(kvp.Key);
            }

            //cleanup
            foreach (var item in removekeys)
            {
                QueuedProcesses.Remove(item);
            }


            //add the processors to the new system
            starsys.ManagerSubpulses.ImportProcDictForEntity(entity, procDict);
        }


        internal void ProcessSystem(DateTime targetDateTime)
        {
            if(targetDateTime < StarSysDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow.
            //the system may need to run several times for a target datetime
            //keep processing the system till we've reached the wanted datetime
            _masterPulseStopwatch.Restart();
            _subpulseTimes = new List<double>();
            _detailedProcessTimes = new Dictionary<string, List<double>>();
            IsProcessing = true;
            while (StarSysDateTime < targetDateTime)
            {
                _subPulseStopwatch.Restart();
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
                _subPulseStopwatch.Stop();
                _subpulseTimes.Add(_subPulseStopwatch.Elapsed.TotalMilliseconds);
            }

            CurrentProcess = "Waiting";
            _masterPulseStopwatch.Stop();
            _fullPulseTimeMS = _masterPulseStopwatch.Elapsed.TotalMilliseconds;
            AddPerfHistory();

            IsProcessing = false;
        }

        private DateTime GetNextInterupt(TimeSpan maxSpan)
        {
            DateTime nextInteruptDateTime = StarSysDateTime + maxSpan;
            if (QueuedProcesses.Keys.Count != 0 && nextInteruptDateTime > QueuedProcesses.Keys.Min())
            {
                nextInteruptDateTime = QueuedProcesses.Keys.Min();
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
            if (QueuedProcesses.ContainsKey(_processToDateTime))
            {
                StarSysDateTime = _processToDateTime; //update the localDateTime and invoke the SystemDateChangedEvent
                var qp = QueuedProcesses[_processToDateTime];
                foreach (var systemProcess in qp.SystemProcessors)
                {
                    _processStopwatch.Restart();
                    CurrentProcess = systemProcess.ToString();
                    int count = systemProcess.ProcessManager(_entityManager, deltaSeconds);
                    _processStopwatch.Stop();
                    //ProcessTime[systemProcess.GetType()] = _processStopwatch.Elapsed;
                    string pname = systemProcess.GetType().Name;
                    if(!_detailedProcessTimes.ContainsKey(pname))
                        _detailedProcessTimes.Add(pname, new List<double>());
                    _detailedProcessTimes[pname].Add(_processStopwatch.Elapsed.TotalMilliseconds);
                    if(count > 0)
                        AddSystemInterupt(_processToDateTime + systemProcess.RunFrequency, systemProcess); //sets the next interupt for this hotloop process
                }

                foreach(var instanceProcessSet in qp.InstanceProcessors)
                {
                    var processor = _processManager.GetInstanceProcessor(instanceProcessSet.Key);
                    _processStopwatch.Restart();
                    CurrentProcess = instanceProcessSet.Key;
                    foreach (var entity in instanceProcessSet.Value)
                    {

                        processor.ProcessEntity(entity, _processToDateTime);
                    }
                    _processStopwatch.Stop();
                    //ProcessTime[processor.GetType()] = _processStopwatch.Elapsed;
                    string pname = processor.GetType().Name;
                    if(!_detailedProcessTimes.ContainsKey(pname))
                        _detailedProcessTimes.Add(pname, new List<double>());
                    _detailedProcessTimes[pname].Add(_processStopwatch.Elapsed.TotalMilliseconds);
                }

                QueuedProcesses.Remove(_processToDateTime); //once all the processes have been run for that datetime, remove it from the dictionary.
            }

        }

        public int GetTotalNumberOfProceses()
        {
            int i = 0;
            foreach (var processSet in QueuedProcesses)
            {
                i += processSet.Value.InstanceProcessors.Count;
                i += processSet.Value.SystemProcessors.Count;
            }

            return i;
        }

        public List<DateTime> GetInteruptDateTimes()
        {
            List<DateTime> dates = new List<DateTime>();
            foreach (var item in QueuedProcesses)
            {
                dates.Add(item.Key);
            }
            return dates;
        }

    }
}


