using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// handles and processes entities for a specific datetime. 
    /// TODO:  handle removal of entities from the system.
    /// TODO:  handle removal of ability datablobs from an entity
    /// TODO:  handle passing an entity from this system to another, and carry it's subpulses/interupts across. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SystemSubPulses : IEquatable<SystemSubPulses>
    {
        //TODO there may be a more efficent datatype for this. 
        [JsonProperty]
        public SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<Entity>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<Entity>>>();


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

        public SystemSubPulses() { }

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
            AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.EconomyCycleTime, PulseActionEnum.EconProcessor);
            AddSystemInterupt(_starSystem.Game.CurrentDateTime + _starSystem.Game.Settings.OrbitCycleTime, PulseActionEnum.OrbitProcessor);
        }


        /// <summary>
        /// adds a system(non pausing) interupt, causing this system to process an entity with a given processor on a specific datetime 
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        internal void AddEntityInterupt(DateTime nextDateTime, PulseActionEnum action, Entity entity)
        {
            if (!EntityDictionary.ContainsKey(nextDateTime))
                EntityDictionary.Add(nextDateTime, new Dictionary<PulseActionEnum, List<Entity>>());
            if (!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, new List<Entity>());
            EntityDictionary[nextDateTime][action].Add(entity);
        }


        /// <summary>
        /// this type of interupt will attempt to run the action processor on all entities within the system
        /// </summary>
        /// <param name="nextDateTime"></param>
        /// <param name="action"></param>
        internal void AddSystemInterupt(DateTime nextDateTime, PulseActionEnum action)
        {
            if (!EntityDictionary.ContainsKey(nextDateTime))
                EntityDictionary.Add(nextDateTime, new Dictionary<PulseActionEnum, List<Entity>>());
            if (!EntityDictionary[nextDateTime].ContainsKey(action))
                EntityDictionary[nextDateTime].Add(action, null);//new List<Entity>());
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
                    foreach (KeyValuePair<PulseActionEnum, List<Entity>> delegateListPair in EntityDictionary[nextInteruptDateTime])
                    {
                        if (delegateListPair.Value == null) //if the list is null, it's a systemwide interupt
                        {
                            //delegateListPair.Key.DynamicInvoke(_starSystem);
                            PulseActionDictionary.DoAction(delegateListPair.Key, _starSystem);
                        }
                        else
                            foreach (Entity entity in delegateListPair.Value) //foreach entity in the value list
                            {
                                //delegateListPair.Key.DynamicInvoke(entity);
                                PulseActionDictionary.DoAction(delegateListPair.Key, _starSystem, entity);
                            }
                    }

                    SystemLocalDateTime = nextInteruptDateTime;
                    EntityDictionary.Remove(nextInteruptDateTime);
                }
                else
                    SystemLocalDateTime += maxSpan;
            }
            else
                SystemLocalDateTime += maxSpan;

            return SystemLocalDateTime;
        }


        public bool Equals(SystemSubPulses other)
        {
            bool equality = false;
            if (SystemLocalDateTime.Equals(other.SystemLocalDateTime))
            {
                if (EntityDictionary.Count.Equals(other.EntityDictionary.Count))
                    equality = true;
            }
            return equality;
        }
    }
}
