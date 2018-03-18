using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Reflection;

namespace Pulsar4X.ECSLib
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
        [JsonProperty]
        private SortedDictionary<DateTime, ProcessSet> QueuedProcesses = new SortedDictionary<DateTime, ProcessSet>();
        class ProcessSet : ISerializable
        {   //TODO: need to figure out how to serialize/Deserialise this
            //the Interface keys are references to the concreate classes in the _processManager
            [JsonIgnore] //this should get added on initialization. 
            internal List<IHotloopProcessor> SystemProcessors = new List<IHotloopProcessor>();
            [JsonProperty] //this needs to get saved. the problem here is saving IInstance Processors, maybe this should be a Type. need to check that entities here are saved as guids in the save file and that they get re-referenced on load too (should happen if the serialization manager does its job properly). 
            internal Dictionary<string, List<Entity>> InstanceProcessors = new Dictionary<string, List<Entity>>();

            internal ProcessSet() { }

            internal ProcessSet(SerializationInfo info, StreamingContext context)
            {
                Game game = (Game)context.Context;
                Dictionary<string, List<Guid>> instanceProcessors = (Dictionary<string, List<Guid>>)info.GetValue(nameof(InstanceProcessors), typeof(Dictionary<string, List<Guid>>));
                ProcessorManager processManager = game.ProcessorManager;
                Entity entity;
                foreach (var kvpItem in instanceProcessors)
                {
                    
                    string typeName = kvpItem.Key;

                    //IInstanceProcessor processor = processManager.GetInstanceProcessor(typeName);
                    if (!InstanceProcessors.ContainsKey(typeName))
                        InstanceProcessors.Add(typeName, new List<Entity>());
                    
                    foreach (var entityGuid in kvpItem.Value)
                    {
                        if (game.GlobalManager.FindEntityByGuid(entityGuid, out entity)) //might be a better way to do this, can we get the manager from here and just search localy?
                        {

                            InstanceProcessors[typeName].Add(entity);
                        }
                        else
                        {
                            // Entity has not been deserialized.
                            throw new Exception("Unfound Entity Exception, possibly this entity hasn't been deseralised yet?"); //I *think* we'll have the entitys all deseralised for this manager at this point...
                        }
                    }
                }
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                Dictionary<string, List<Guid>> instanceProcessors = new Dictionary<string, List<Guid>>();
                foreach (var kvpitem in InstanceProcessors)
                {
                    string typeName = kvpitem.Key;//.TypeName;
                    instanceProcessors.Add(typeName, new List<Guid>());
                    foreach (var entityItem in kvpitem.Value)
                    {
                        instanceProcessors[typeName].Add(entityItem.Guid);
                    }
                }
                info.AddValue(nameof(InstanceProcessors), instanceProcessors);
            }
        }


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
            //Event logevent = new Event(_systemLocalDateTime, "System Date Change", _entityManager.Guid, null, null, null);
            //logevent.EventType = EventType.SystemDateChange;
            //_entityManager.Game.EventLog.AddEvent(logevent);

            SystemDateChangedEvent?.Invoke(SystemLocalDateTime);
        }


        [JsonProperty]
        private DateTime _systemLocalDateTime;
        public DateTime SystemLocalDateTime
        {
            get { return _systemLocalDateTime; }
            private set
            {
                if (value < _systemLocalDateTime)
                    throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow. 
                _systemLocalDateTime = value;
                if (_entityManager.Game.SyncContext != null)
                    _entityManager.Game.SyncContext.Post(InvokeDateChange, value);//marshal to the main (UI) thread, so the event is invoked on that thread.
                else //if context is null, we're probibly running tests or headless.
                    InvokeDateChange(value); //in this case we're not going to marshal this. (event will fire on *THIS* thread)   
            }
        }

        /// <summary>
        /// constructor for json
        /// </summary>
        public ManagerSubPulse() 
        {

        }



        internal void Initalise(EntityManager entityManager)//possibly this stuff should be done outside of the class, however it does give some good examples of how to add an interupt...
        {
            _entityManager = entityManager;
            _processManager = entityManager.Game.ProcessorManager;
            if (entityManager.Game != null) //this is needed due to the TreeHirachy test... it's the only place that calls this with an empty game now. TODO: check this is still needed since refactoring
            {
                _systemLocalDateTime = entityManager.Game.CurrentDateTime;
            }

            _processManager.InitializeMangerSubpulse(entityManager);
        }

        internal void Initalise(StreamingContext context, EntityManager entityManager)
        {
            _entityManager = entityManager;
            _processManager = entityManager.Game.ProcessorManager;
            _processManager.InitializeMangerSubpulse(entityManager);
        }

        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime 
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, string actionProcessor, Entity entity)
        {
            if(!QueuedProcesses.ContainsKey(nextDateTime))
                QueuedProcesses.Add(nextDateTime, new ProcessSet());
            if(!QueuedProcesses[nextDateTime].InstanceProcessors.ContainsKey(actionProcessor))
                QueuedProcesses[nextDateTime].InstanceProcessors.Add(actionProcessor, new List<Entity>());
            if(!QueuedProcesses[nextDateTime].InstanceProcessors[actionProcessor].Contains(entity))
                QueuedProcesses[nextDateTime].InstanceProcessors[actionProcessor].Add(entity);                
        }


        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, IHotloopProcessor actionProcessor)
        {
            if(!QueuedProcesses.ContainsKey(nextDateTime))
                QueuedProcesses.Add(nextDateTime, new ProcessSet());
            if(!QueuedProcesses[nextDateTime].SystemProcessors.Contains(actionProcessor))
                QueuedProcesses[nextDateTime].SystemProcessors.Add(actionProcessor);               
        }

        /// <summary>
        /// removes all references of an entity from the dictionary
        /// </summary>
        /// <param name="entity"></param>
        internal void RemoveEntity(Entity entity)
        {
            //possibly need to implement a reverse dictionary so entities can be looked up backwards, rather than itterating through?
            //MUST remove empty entries in the dictionary as an empty entitylist will be seen as a systemInterupt. 
            throw new NotImplementedException();
        }

        /// <summary>
        /// transfers all references from this dictionary to the new one
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="starsys"></param>
        internal void TransferEntity(Entity entity, StarSystem starsys)
        {
            throw new NotImplementedException();
        }


        internal void ProcessSystem(DateTime targetDateTime)
        {
            if(targetDateTime < SystemLocalDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow. 
            //the system may need to run several times for a target datetime
            //keep processing the system till we've reached the wanted datetime
            while (SystemLocalDateTime < targetDateTime)
            {
                //calculate max time the system can run/time to next interupt
                //this should handle predicted events, ie econ, production, shipjumps, sensors etc.
                TimeSpan timeDeltaMax = targetDateTime - SystemLocalDateTime;
                DateTime nextDate = GetNextInterupt(timeDeltaMax);

                TimeSpan deltaActual = nextDate - SystemLocalDateTime;

                //ShipMovementProcessor.Process(_entityManager, (int)deltaActual.TotalSeconds); //process movement for any entity that can move (not orbit)
                _entityManager.Game.ProcessorManager.Hotloop<PropulsionDB>(_entityManager, (int)deltaActual.TotalSeconds);
                ProcessToNextInterupt(nextDate);


            }
        }

        private DateTime GetNextInterupt(TimeSpan maxSpan)
        {
            DateTime nextInteruptDateTime = SystemLocalDateTime + maxSpan;
            if (QueuedProcesses.Keys.Count != 0 && nextInteruptDateTime > QueuedProcesses.Keys.Min())
            {
                nextInteruptDateTime = QueuedProcesses.Keys.Min();
            }
            if (nextInteruptDateTime < SystemLocalDateTime)
                throw new Exception("Temproal Anomaly Exception. Cannot go back in time!"); //because this was actualy happening somehow. 
            return nextInteruptDateTime;
        }

        /// <summary>
        /// process to next subpulse
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="maxSpan">maximum time delta</param>
        /// <returns>datetime processed to</returns>
        private void ProcessToNextInterupt(DateTime nextInteruptDateTime)
        {
            TimeSpan span = (nextInteruptDateTime - _systemLocalDateTime);
            int deltaSeconds = (int)span.TotalSeconds;
            if (QueuedProcesses.ContainsKey(nextInteruptDateTime))
            {

                foreach(var systemProcess in QueuedProcesses[nextInteruptDateTime].SystemProcessors)
                {
                    systemProcess.ProcessManager(_entityManager, deltaSeconds);
                    AddSystemInterupt(nextInteruptDateTime + systemProcess.RunFrequency, systemProcess); //sets the next interupt for this hotloop process
                }
                foreach(var instanceProcessSet in QueuedProcesses[nextInteruptDateTime].InstanceProcessors)
                {
                    foreach(var entity in instanceProcessSet.Value)
                    {
                        var processor = _processManager.GetInstanceProcessor(instanceProcessSet.Key);
                        processor.ProcessEntity(entity, deltaSeconds);
                    }
                }
                QueuedProcesses.Remove(nextInteruptDateTime); //once all the processes have been run for that datetime, remove it from the dictionary. 
            }
            SystemLocalDateTime = nextInteruptDateTime; //update the localDateTime and invoke the SystemDateChangedEvent                   
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


