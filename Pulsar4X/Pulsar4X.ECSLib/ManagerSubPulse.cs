using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        
        private SortedDictionary<DateTime, ProcessSet> QueuedProcesses = new SortedDictionary<DateTime, ProcessSet>();
        class ProcessSet
        {   //TODO: need to figure out how to serialize/Deserialise this
            //the Interface keys are references to the concreate classes in the _processManager
            internal List<IHotloopProcessor> SystemProcessors = new List<IHotloopProcessor>();
            internal Dictionary<IInstanceProcessor, List<Entity>> InstanceProcessors = new Dictionary<IInstanceProcessor, List<Entity>>();
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
        public ManagerSubPulse() { }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="entityManager"></param>
        internal ManagerSubPulse(EntityManager entityManager)
        {
                
            _entityManager = entityManager;
            _processManager = entityManager.Game.ProcessorManager;
            if (entityManager.Game != null) //this is needed due to the TreeHirachy test... it's the only place that calls this with an empty game now. 
            {
                _systemLocalDateTime = _entityManager.Game.CurrentDateTime;
                _entityManager.Game.PostLoad += Game_PostLoad;
            }

        }

        private void Game_PostLoad(object sender, EventArgs e)
        {
            //Initalise();
        }

        internal void Initalise()//possibly this stuff should be done outside of the class, however it does give some good examples of how to add an interupt...
        {
            //we offset some of these to spread the load out a bit more. 
            //TODO: move this stuff to the processorManager
            AddSystemInterupt(_entityManager.Game.CurrentDateTime, _processManager.GetProcessor<OrbitDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime, _processManager.GetProcessor<NewtonBalisticDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime, _processManager.GetProcessor<EntityResearchDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromMinutes(5), _processManager.GetProcessor<OrderableDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromMinutes(10), _processManager.GetProcessor<TranslateMoveDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromMinutes(10.1), _processManager.GetProcessor<SensorSigDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromHours(1), _processManager.GetProcessor<MiningDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromHours(2), _processManager.GetProcessor<RefiningDB>());
            AddSystemInterupt(_entityManager.Game.CurrentDateTime + TimeSpan.FromHours(3), _processManager.GetProcessor<ConstructionDB>());


        }


        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime 
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, IInstanceProcessor actionProcessor, Entity entity)
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


        internal void ProcessSystem(DateTime toDateTime)
        {
            //check validity of commands etc. here.


            //the system may need to run several times for a wanted tickLength
            //keep processing the system till we've reached the wanted ticklength
            while (SystemLocalDateTime < toDateTime)
            {

                //calculate max time the system can run/time to next interupt
                //this should handle predicted events, ie econ, production, shipjumps, sensors etc.
                TimeSpan timeDeltaMax = toDateTime - SystemLocalDateTime;
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
                    AddSystemInterupt(nextInteruptDateTime + systemProcess.RunFrequency, systemProcess);
                }
                foreach(var instanceProcessSet in QueuedProcesses[nextInteruptDateTime].InstanceProcessors)
                {
                    foreach(var entity in instanceProcessSet.Value)
                    {
                        instanceProcessSet.Key.ProcessEntity(entity, deltaSeconds);
                    }
                }
                QueuedProcesses.Remove(nextInteruptDateTime);
            }
            SystemLocalDateTime = nextInteruptDateTime; //update the localDateTime and invoke the SystemDateChangedEvent            
        }

    }
}


